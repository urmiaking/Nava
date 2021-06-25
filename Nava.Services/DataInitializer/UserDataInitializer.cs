using Nava.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Nava.Entities.User;

namespace Nava.Services.DataInitializer
{
    public class UserDataInitializer : IDataInitializer
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserDataInitializer(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
        }
    }
}