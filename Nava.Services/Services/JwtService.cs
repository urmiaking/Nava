using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nava.Common;
using Nava.Common.Utilities;
using Nava.Entities;
using Nava.Entities.User;
using Role = Nava.Entities.User.Role;

namespace Nava.Services.Services
{
    public class JwtService : IJwtService, IScopedDependency
    {
        private readonly SiteSettings _siteSettings;
        private readonly UserManager<User> _userManager;

        public JwtService(IOptionsSnapshot<SiteSettings> settings, UserManager<User> userManager)
        {
            _userManager = userManager;
            _siteSettings = settings.Value;
        }
        public async Task<AccessToken> GenerateAsync(User user)
        {
            var secretKey = Encoding.UTF8.GetBytes(_siteSettings.JwtSettings.SecretKey); // longer than 16 character
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

            var encryptionKey = Encoding.UTF8.GetBytes(_siteSettings.JwtSettings.EncryptKey); // must be 16 characters
            var encryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(encryptionKey), SecurityAlgorithms.Aes128KW, SecurityAlgorithms.Aes128CbcHmacSha256);

            var claims = await GetClaimsAsync(user);
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _siteSettings.JwtSettings.Issuer,
                Audience = _siteSettings.JwtSettings.Audience,
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now.AddMinutes(_siteSettings.JwtSettings.NotBeforeMinutes),
                Expires = DateTime.Now.AddMinutes(_siteSettings.JwtSettings.ExpirationMinutes),
                SigningCredentials = signingCredentials,
                Subject = new ClaimsIdentity(claims),
                EncryptingCredentials = encryptingCredentials
            };

            // Clearing default claim type mapping to jwt claim type
            /*
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
            */

            var tokenHandler = new JwtSecurityTokenHandler();

            var securityToken = tokenHandler.CreateJwtSecurityToken(descriptor);

            //var jwt = tokenHandler.WriteToken(securityToken);

            return new AccessToken(securityToken);
        }

        public AccessToken GenerateForMongo(Entities.MongoDb.User user)
        {
            var secretKey = Encoding.UTF8.GetBytes(_siteSettings.JwtSettings.SecretKey);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

            var encryptionKey = Encoding.UTF8.GetBytes(_siteSettings.JwtSettings.EncryptKey);
            var encryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(encryptionKey), SecurityAlgorithms.Aes128KW, SecurityAlgorithms.Aes128CbcHmacSha256);

            var claims = GetMongoClaims(user);
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _siteSettings.JwtSettings.Issuer,
                Audience = _siteSettings.JwtSettings.Audience,
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now.AddMinutes(_siteSettings.JwtSettings.NotBeforeMinutes),
                Expires = DateTime.Now.AddMinutes(_siteSettings.JwtSettings.ExpirationMinutes),
                SigningCredentials = signingCredentials,
                Subject = new ClaimsIdentity(claims),
                EncryptingCredentials = encryptingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var securityToken = tokenHandler.CreateJwtSecurityToken(descriptor);

            return new AccessToken(securityToken);
        }

        private async Task<IEnumerable<Claim>> GetClaimsAsync(User user)
        {
            var securityStampClaimType = new ClaimsIdentityOptions().SecurityStampClaimType;
            var list = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(securityStampClaimType, user.SecurityStamp)
            };

            list.AddRange(from role in await _userManager.GetRolesAsync(user)
                select new Claim(ClaimTypes.Role, role));

            return list;
        }

        private IEnumerable<Claim> GetMongoClaims(Entities.MongoDb.User user)
        {
            var securityStampClaimType = new ClaimsIdentityOptions().SecurityStampClaimType;
            var list = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(securityStampClaimType, user.SecurityStamp)
            };

            list.AddRange(from role in user.Roles
                select new Claim(ClaimTypes.Role, role));

            return list;
        }

    }
}
