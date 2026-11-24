using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace DC_BOUTIQUE.Infrastructure.Identity.Seeds
{
    public static class DefaultAdminUser
    {
        public static async Task SeedAsync(UserManager<AppUser> userManager)
        {
            var defaultUser = new AppUser
            {
                UserName = "admin",
                FirstName = "Dahiana",
                LastName = "Corporan",
                Cedula = "402-0000000-0",
                PhoneNumber = "809-555-0101",
                IsActive = true
            };

            if (await userManager.FindByNameAsync(defaultUser.UserName) == null)
            {
                var result = await userManager.CreateAsync(defaultUser, "Admin123$");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Administrador");
                }
            }
        }
    }
}
