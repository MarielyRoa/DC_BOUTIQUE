using DC_BOUTIQUE.Core.Domain.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class Venta : BasicEntity
    {
        public required string NumeroFactura { get; set; }
        public string? Ncf { get; set; }
        public TipoComprobante TipoComprobante { get; set; } = TipoComprobante.TicketInterno;

        public Guid? ClienteId { get; set; }
        public Guid UsuarioId { get; set; }
        public Guid TurnoCajaId { get; set; }

        public DateTime FechaVenta { get; set; } = DateTime.UtcNow;

        public decimal Subtotal { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal ImpuestoTotal { get; set; }
        public decimal TotalNeto { get; set; }

        public EstadoVenta Estado { get; set; } = EstadoVenta.Completada;
        public string? Observaciones { get; set; }

        // Propiedades de navegación limpias
        public Cliente? Cliente { get; set; }
        public Usuario? Usuario { get; set; }
        public TurnoCaja? TurnoCaja { get; set; }
        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
        public ICollection<PagoVenta> Pagos { get; set; } = new List<PagoVenta>();
    }
}
