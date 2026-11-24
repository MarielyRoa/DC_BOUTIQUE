using Microsoft.AspNetCore.Identity;

namespace DC_BOUTIQUE.Infrastructure.Identity.Seeds
{
    public static class DefaultUserRoles
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Administrador"))
            {
                await roleManager.CreateAsync(new IdentityRole("Administrador"));
            }

            if (!await roleManager.RoleExistsAsync("Vendedor"))
            {
                await roleManager.CreateAsync(new IdentityRole("Vendedor"));
            }

            if (!await roleManager.RoleExistsAsync("Cajero"))
            {
                await roleManager.CreateAsync(new IdentityRole("Cajero"));
            }
        }
    }
}
