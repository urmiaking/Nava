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
            
            var adminUserList = _userManager.GetUsersInRoleAsync(Common.Role.Admin).GetAwaiter().GetResult().ToList();

            if (adminUserList.Count == 0)
            {
                var user = new User
                {
                    FullName = "ادمین",
                    UserName = "admin",
                    Email = "admin@admin.com",
                    Bio = "مدیر اپلیکیشن نوا"
                };
                _userManager.CreateAsync(user, "masoud").GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(user, Common.Role.Admin).GetAwaiter().GetResult();
            }

            var mongoAdmin = _mongoRepository.FindOne(a => 
                a.Id == ObjectId.Parse("AAAAAAAAAAAAAAAAAAAAAAAA"));

            if (mongoAdmin is null)
            {
                var mongoUser = new Entities.MongoDb.User
                {
                    FullName = "ادمین",
                    UserName = "admin",
                    PasswordHash = "25AXN8QeSQ3si97ZE/ES5efHIMOEdVjw5cZRKL2xs0w=",
                    IsActive = true,
                    Id = ObjectId.Parse("AAAAAAAAAAAAAAAAAAAAAAAA"),
                    Roles = new List<string> {Common.Role.Admin},
                    Bio = "مدیر اپلیکیشن نوا"
                };
                _mongoRepository.InsertOne(mongoUser);
            }

        }
    }
}