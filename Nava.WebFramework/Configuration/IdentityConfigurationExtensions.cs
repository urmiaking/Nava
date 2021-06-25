using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Nava.Common;
using Nava.Data;
using Nava.Entities;
using Nava.Entities.User;

namespace Nava.WebFramework.Configuration
{
    public static class IdentityConfigurationExtensions
    {
        public static void AddCustomIdentity(this IServiceCollection services, IdentitySettings settings)
        {
            services.AddIdentity<User, Entities.User.Role>(identityOptions =>
            {
                // Password Settings
                identityOptions.Password.RequireDigit = settings.PasswordRequireDigit;
                identityOptions.Password.RequiredLength = settings.PasswordRequiredLength;
                identityOptions.Password.RequireLowercase = settings.PasswordRequireLowercase;
                identityOptions.Password.RequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric;
                identityOptions.Password.RequireUppercase = settings.PasswordRequireUppercase;

                // Username Settings
                identityOptions.User.RequireUniqueEmail = settings.EmailRequireUniqueEmail;

                // SignIn Settings
                //identityOptions.SignIn.RequireConfirmedPhoneNumber = false;
                //identityOptions.SignIn.RequireConfirmedEmail = false;

                // Lockout Settings : only useful when using cookies with signInManager
                //identityOptions.Lockout.MaxFailedAccessAttempts = 5;
                //identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                //identityOptions.Lockout.AllowedForNewUsers = false;
            }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        }
    }
}
