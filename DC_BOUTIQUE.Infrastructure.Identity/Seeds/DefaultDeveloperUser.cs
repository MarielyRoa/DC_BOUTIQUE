using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace DC_BOUTIQUE.Infrastructure.Identity.Seeds
{
    /// <summary>
    /// Cuenta técnica inicial del rol Desarrollador para soporte y mantenimiento de instalaciones locales.
    /// Oculta e ineditable desde el panel administrativo de la boutique.
    /// </summary>
    public static class DefaultDeveloperUser
    {
        public const string DefaultUserName = "desarrollador";
        public const string DefaultPassword = "DevBoutique2026$";
        public const string DefaultFirstName = "Soporte";
        public const string DefaultLastName = "Desarrollador";

        public static async Task SeedAsync(UserManager<AppUser> userManager)
        {
            var existingDevs = await userManager.GetUsersInRoleAsync("Desarrollador");
            if (existingDevs.Any())
            {
                return;
            }

            var existingByName = await userManager.FindByNameAsync(DefaultUserName);
            if (existingByName != null)
            {
                if (!await userManager.IsInRoleAsync(existingByName, "Desarrollador"))
                {
                    await userManager.AddToRoleAsync(existingByName, "Desarrollador");
                }
                return;
            }

            var defaultUser = new AppUser
            {
                UserName = DefaultUserName,
                FirstName = DefaultFirstName,
                LastName = DefaultLastName,
                IsActive = true
            };

            var result = await userManager.CreateAsync(defaultUser, DefaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultUser, "Desarrollador");
            }
        }
    }
}
