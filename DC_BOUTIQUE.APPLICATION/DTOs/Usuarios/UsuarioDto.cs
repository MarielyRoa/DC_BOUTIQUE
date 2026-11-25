using DC_BOUTIQUE.Core.Application.DTOs.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Application.DTOs.Usuarios
{
    public class UsuarioDto : BasicDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public RolUsuario Rol { get; set; }
        public bool EsPrincipal { get; set; }
        public string NombreRol => Rol.ToString();
        public decimal PorcentajeComision { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }
}
