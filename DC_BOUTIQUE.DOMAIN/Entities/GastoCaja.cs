using DC_BOUTIQUE.Core.Domain.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class GastoCaja : BasicEntity
    {
        public Guid? TurnoCajaId { get; set; }
        public Guid UsuarioId { get; set; }

        public required string Concepto { get; set; }
        public required string CategoriaGasto { get; set; } // Ej. Bolsas/Empaques boutique, Suministros, Limpieza
        public decimal Monto { get; set; }
        public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
        public string? ComprobanteReferencia { get; set; }
        public string? Observaciones { get; set; }

        // Propiedades de navegación
        public TurnoCaja? TurnoCaja { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
