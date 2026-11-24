using DC_BOUTIQUE.Core.Domain.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    /// <summary>
    /// Permite soportar múltiples métodos de pago por venta (ej. efectivo + tarjeta, o pago mixto).
    /// </summary>
    public class PagoVenta : BasicEntity
    {
        public Guid VentaId { get; set; }

        public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
        public decimal Monto { get; set; }
        public string? ReferenciaTransaccion { get; set; }

        // Propiedades de navegación
        public Venta? Venta { get; set; }
    }
}
