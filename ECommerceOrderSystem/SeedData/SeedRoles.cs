using Microsoft.AspNetCore.Identity;

namespace ECommerceOrderSystem.SeedData
{
    public class SeedData
    {
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
    }
}
