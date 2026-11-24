using DC_BOUTIQUE.Core.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Usuarios
{
    public class SaveUsuarioDto
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        public string? Password { get; set; }

        [StringLength(20)]
        public string? Cedula { get; set; }

        [StringLength(25)]
        public string? Telefono { get; set; }

        public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;

        [Range(0, 100, ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
        public decimal PorcentajeComision { get; set; } = 0m;

        public bool Activo { get; set; } = true;
    }
}
