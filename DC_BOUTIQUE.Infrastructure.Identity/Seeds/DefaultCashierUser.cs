using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace DC_BOUTIQUE.Infrastructure.Identity.Seeds
{
    public static class DefaultCashierUser
    {
        public static async Task SeedAsync(UserManager<AppUser> userManager)
        {
            var defaultUser = new AppUser
            {
                UserName = "caja01",
                FirstName = "Cajera",
                LastName = "Principal",
                Cedula = "402-0000000-1",
                PhoneNumber = "809-555-0102",
                PorcentajeComision = 2.0m,
                IsActive = true
            };

            if (await userManager.FindByNameAsync(defaultUser.UserName) == null)
            {
                var result = await userManager.CreateAsync(defaultUser, "Caja1234$");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Cajero");
                }
            }
        }
    }
}
