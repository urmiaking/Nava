using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nava.Common;
using Nava.Common.Exceptions;
using Nava.Common.Utilities;
using Nava.Data;
using Nava.Data.Contracts;
using Nava.Entities.User;

namespace Nava.WebFramework.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("SqlServer")));
        }

        public static void AddMinimalControllers(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
        }

        public static void AddJwtAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                var secretKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
                var encryptionKey = Encoding.UTF8.GetBytes(jwtSettings.EncryptKey);

                var validationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero, // default: 5 min
                    RequireSignedTokens = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),

                    RequireExpirationTime = true,
                    ValidateLifetime = true,

                    ValidateAudience = true, //default : false
                    ValidAudience = jwtSettings.Audience,

                    ValidateIssuer = true, //default : false
                    ValidIssuer = jwtSettings.Issuer,

                    TokenDecryptionKey = new SymmetricSecurityKey(encryptionKey)
                };

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = validationParameters;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                        logger.LogError("Authentication failed.", context.Exception);

                        if (context.Exception != null)
                            throw new AppException(ApiResultStatusCode.UnAuthorized, "Authentication failed.", HttpStatusCode.Unauthorized, context.Exception, null);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var url = context.Request.GetDisplayUrl();

                        if (url.Contains("v1"))
                        {
                            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();
                            var userRepository = context.HttpContext.RequestServices.GetRequiredService<IRepository<User>>();

                            var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                            if (claimsIdentity.Claims?.Any() != true)
                                context.Fail("This token has no claims.");

                            var securityStamp = await signInManager.ValidateSecurityStampAsync(context.Principal);

                            if (securityStamp == null)
                            {
                                context.Fail("This token has no security stamp");
                            }

                            //var securityStamp = claimsIdentity.FindFirstValue(new ClaimsIdentityOptions().SecurityStampClaimType);
                            //if (!securityStamp.HasValue())
                            // context.Fail("This token has no security stamp");

                            //Find user and token from database and perform your custom validation
                            var userId = claimsIdentity.GetUserId<int>();
                            var user = await userRepository.GetByIdAsync(context.HttpContext.RequestAborted, userId);

                            //if (user.MySecurityStamp != Guid.Parse(securityStamp)) //TODO: Change to securityStamp
                            //    context.Fail("Token security stamp is not valid.");

                            var validatedUser = await signInManager.ValidateSecurityStampAsync(context.Principal);
                            if (validatedUser == null)
                                context.Fail("Token security stamp is not valid.");

                            if (!user.IsActive)
                                context.Fail("User is not active.");
                        }

                        
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                        logger.LogError("OnChallenge error", context.Error, context.ErrorDescription);

                        if (context.AuthenticateFailure != null)
                            throw new AppException(ApiResultStatusCode.UnAuthorized, "Authenticate failure.", HttpStatusCode.Unauthorized, context.AuthenticateFailure, null);
                        throw new AppException(ApiResultStatusCode.UnAuthorized, "You are unauthorized to access this resource.", HttpStatusCode.Unauthorized);
                    }
                };
            });
        }

        public static void AddCustomApiVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                //url segment => {version}
                options.AssumeDefaultVersionWhenUnspecified = true; //default => false;
                options.DefaultApiVersion = new ApiVersion(1, 0); //v1.0 == v1
                options.ReportApiVersions = true;

                //ApiVersion.TryParse("1.0", out var version10);
                //ApiVersion.TryParse("1", out var version1);
                //var a = version10 == version1;

                //options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
                // api/posts?api-version=1

                //options.ApiVersionReader = new UrlSegmentApiVersionReader();
                // api/v1/posts

                //options.ApiVersionReader = new HeaderApiVersionReader(new[] { "Api-Version" });
                // header => Api-Version : 1

                //options.ApiVersionReader = new MediaTypeApiVersionReader()

                //options.ApiVersionReader = ApiVersionReader.Combine(new QueryStringApiVersionReader("api-version"), new UrlSegmentApiVersionReader())
                // combine of [querystring] & [urlsegment]
            });
        }

        public static void ConfigMongoDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbDatabaseSettings>(
                configuration.GetSection(nameof(MongoDbDatabaseSettings)));

            services.AddSingleton<IMongoDbDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<MongoDbDatabaseSettings>>().Value);
        }
    }
}