using DC_BOUTIQUE.Core.Application.ViewModels.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Application.ViewModels.Usuarios
{
    public class UsuarioViewModel : BasicViewModel
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public RolUsuario Rol { get; set; }
        public string NombreRol => Rol.ToString();
        public decimal PorcentajeComision { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }
}
