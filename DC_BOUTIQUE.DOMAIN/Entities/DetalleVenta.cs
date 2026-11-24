using DC_BOUTIQUE.Core.Domain.Common;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class DetalleVenta : BasicEntity
    {
        public Guid VentaId { get; set; }
        public Guid ProductoVarianteId { get; set; }

        // Snapshots históricos para conservar la integridad exacta de la venta
        public required string SkuSnapshot { get; set; }
        public required string NombreProductoSnapshot { get; set; }
        public required string TallaSnapshot { get; set; }
        public required string ColorSnapshot { get; set; }

        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal MontoDescuento { get; set; }
        public decimal MontoImpuesto { get; set; }
        public decimal SubtotalLinea { get; set; }

        // Propiedades de navegación
        public Venta? Venta { get; set; }
        public ProductoVariante? ProductoVariante { get; set; }
    }
}
