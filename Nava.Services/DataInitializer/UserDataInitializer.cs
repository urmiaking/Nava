using System;
using System.Collections.Generic;
using Nava.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MongoDB.Bson;
using Nava.Data.Contracts;
using Nava.Entities.User;

namespace Nava.Services.DataInitializer
{
    public class UserDataInitializer : IDataInitializer
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IMongoRepository<Entities.MongoDb.User> _mongoRepository;

        public UserDataInitializer(UserManager<User> userManager, RoleManager<Role> roleManager, IMongoRepository<Entities.MongoDb.User> mongoRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mongoRepository = mongoRepository;
        }

        public void InitializeData()
        {
            if (!_roleManager.RoleExistsAsync(Common.Role.Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new Role { Name = Common.Role.Admin, Description = "Admin role" }).GetAwaiter().GetResult();
            }
            if (!_userManager.Users.AsNoTracking().Any(p => p.NormalizedUserName == "ADMIN"))
            {
                var user = new User
                {
                    FullName = "ادمین سایت",
                    UserName = "admin",
                    Email = "admin@admin.com"
                };
                _userManager.CreateAsync(user, "masoud").GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(user, Common.Role.Admin).GetAwaiter().GetResult();
            }

            var mongoAdmin = _mongoRepository.FindOne(a => a.UserName.Equals("admin"));

            if (mongoAdmin is null)
            {
                var mongoUser = new Entities.MongoDb.User
                {
                    FullName = "ادمین سایت",
                    UserName = "admin",
                    PasswordHash = "25AXN8QeSQ3si97ZE/ES5efHIMOEdVjw5cZRKL2xs0w=",
                    IsActive = true,
                    Id = ObjectId.GenerateNewId(DateTime.Now),
                    Roles = new List<string> {Common.Role.Admin},
                    Bio = "مدیریت API"
                };
                _mongoRepository.InsertOne(mongoUser);
            }

        }
    }
}