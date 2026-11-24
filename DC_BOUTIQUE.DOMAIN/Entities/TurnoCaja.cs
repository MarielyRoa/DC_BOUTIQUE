using DC_BOUTIQUE.Core.Domain.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class TurnoCaja : BasicEntity
    {
        public required string CodigoTurno { get; set; }
        public Guid UsuarioId { get; set; }

        public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
        public DateTime? FechaCierre { get; set; }

        public decimal FondoInicialEfectivo { get; set; }
        public decimal? EfectivoContadoAlCierre { get; set; }
        public decimal? DiferenciaArqueo { get; set; }

        public EstadoCaja Estado { get; set; } = EstadoCaja.Abierta;
        public string? Observaciones { get; set; }

        // Propiedades de navegación
        public Usuario? Usuario { get; set; }
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<GastoCaja> Gastos { get; set; } = new List<GastoCaja>();
    }
}
