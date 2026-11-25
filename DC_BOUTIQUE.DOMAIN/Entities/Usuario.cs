using DC_BOUTIQUE.Core.Domain.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class Usuario : BasicEntity
    {
        public required string NombreCompleto { get; set; }
        public required string NombreUsuario { get; set; }
        public required string PasswordHash { get; set; }
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
        public bool EsPrincipal { get; set; } = false;
        public decimal PorcentajeComision { get; set; } = 0m;
        public DateTime? UltimoAcceso { get; set; }

        // Propiedades de navegación
        public ICollection<Venta> VentasRealizadas { get; set; } = new List<Venta>();
        public ICollection<TurnoCaja> TurnosAperturados { get; set; } = new List<TurnoCaja>();
        public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
    }
}
