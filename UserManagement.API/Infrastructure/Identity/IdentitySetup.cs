using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Core.Entities;
using UserManagement.Infrastructure.Data;

namespace UserManagement.Infrastructure.Identity
{
    public static class IdentitySetup
    {
        public static IServiceCollection AddIdentitySetup(this IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }
    }
}
