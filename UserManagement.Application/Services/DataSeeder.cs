// File: UserManagement.Application/Services/DataSeeder.cs
using Microsoft.AspNetCore.Identity;
using UserManagement.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace UserManagement.Application.Services
{
    public class DataSeeder
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public DataSeeder(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // Seed roles
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new Role { Name = role });
                }
            }
        }
    }
}
