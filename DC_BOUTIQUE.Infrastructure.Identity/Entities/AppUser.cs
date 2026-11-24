using Microsoft.AspNetCore.Identity;

namespace DC_BOUTIQUE.Infrastructure.Identity.Entities
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public decimal PorcentajeComision { get; set; } = 0m;
        public bool IsActive { get; set; } = true;
    }
}
