using DC_BOUTIQUE.Core.Application.ViewModels.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.ViewModels.Ventas
{
    public class VentaViewModel : BasicViewModel
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

        public List<DetalleVentaViewModel> Detalles { get; set; } = new();
        public List<PagoVentaViewModel> Pagos { get; set; } = new();
    }

    public class DetalleVentaViewModel : BasicViewModel
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

    public class PagoVentaViewModel : BasicViewModel
    {
        public Guid VentaId { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public string NombreMetodoPago => MetodoPago.ToString();
        public decimal Monto { get; set; }
        public string? ReferenciaTransaccion { get; set; }
    }

    public class RegistrarVentaViewModel
    {
        public Guid? ClienteId { get; set; }
        public Guid TurnoCajaId { get; set; }
        public TipoComprobante TipoComprobante { get; set; } = TipoComprobante.TicketInterno;
        public string? RncCliente { get; set; }
        public string? RazonSocialCliente { get; set; }
        public string? Observaciones { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "El carrito no puede estar vacío.")]
        public List<ItemCarritoViewModel> Items { get; set; } = new();

        [Required]
        [MinLength(1, ErrorMessage = "Debe registrar al menos un pago.")]
        public List<PagoItemViewModel> Pagos { get; set; } = new();
    }

    public class ItemCarritoViewModel
    {
        public Guid ProductoVarianteId { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal Subtotal => (PrecioUnitario * Cantidad) * (1 - (PorcentajeDescuento / 100m));
    }

    public class PagoItemViewModel
    {
        public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
        public decimal Monto { get; set; }
        public string? Referencia { get; set; }
    }
}
