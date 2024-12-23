using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Domain.Entities;
using UserManagement.Infrastructure.Data;

namespace UserManagement.Infrastructure.Identity
{
    public static class IdentitySetup
    {
        public static IServiceCollection AddIdentitySetup(this IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 1;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<UserManagementDbContext>()
            .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
            .AddDefaultTokenProviders();

            return services;
        }
    }
}

