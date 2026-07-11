using ECommerceOrderSystem.Common;
using Microsoft.AspNetCore.Identity;

namespace ECommerceOrderSystem.SeedData
{
    public class SeedData
    {
        //seed roles
        public async static Task SeedRoles(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var roles = new List<IdentityRole>
            {
                new IdentityRole {Name="ADMIN", NormalizedName="ADMIN"},
                new IdentityRole {Name="CUSTOMER", NormalizedName="CUSTOMER"},
            };

            foreach(var role in roles)
            {
                if(!await roleManager.RoleExistsAsync(role.Name))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        //Seed default admin user
        public async static Task SeedAdminUser(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminEmail = configuration["Admin:Email"];
            var adminPassword = configuration["Admin:Password"];
            if(await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = configuration["Admin:FullName"],
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if(result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "ADMIN");
                }
            }
        }
    }
}
