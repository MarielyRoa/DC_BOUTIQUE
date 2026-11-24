using DC_BOUTIQUE.Core.Application.DTOs.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Application.DTOs.Ventas
{
    public class VentaDto : BasicDto
    {
        public string NumeroFactura { get; set; } = string.Empty;
        public string? Ncf { get; set; }
        public TipoComprobante TipoComprobante { get; set; }

        public Guid? ClienteId { get; set; }
        public string? NombreCliente { get; set; }

        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;

        public Guid TurnoCajaId { get; set; }
        public DateTime FechaVenta { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal ImpuestoTotal { get; set; }
        public decimal TotalNeto { get; set; }

        public EstadoVenta Estado { get; set; }
        public string? Observaciones { get; set; }

        public List<DetalleVentaDto> Detalles { get; set; } = new();
        public List<PagoVentaDto> Pagos { get; set; } = new();
    }

    public class DetalleVentaDto : BasicDto
    {
        public Guid VentaId { get; set; }
        public Guid ProductoVarianteId { get; set; }

        public string SkuSnapshot { get; set; } = string.Empty;
        public string NombreProductoSnapshot { get; set; } = string.Empty;
        public string TallaSnapshot { get; set; } = string.Empty;
        public string ColorSnapshot { get; set; } = string.Empty;

        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal MontoDescuento { get; set; }
        public decimal MontoImpuesto { get; set; }
        public decimal SubtotalLinea { get; set; }
    }

    public class PagoVentaDto : BasicDto
    {
        public Guid VentaId { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public string NombreMetodoPago => MetodoPago.ToString();
        public decimal Monto { get; set; }
        public string? ReferenciaTransaccion { get; set; }
    }
}
