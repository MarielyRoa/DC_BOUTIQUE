using DC_BOUTIQUE.Core.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Ventas
{
    public class RegistrarVentaDto
    {
        public Guid? ClienteId { get; set; }
        public Guid TurnoCajaId { get; set; }
        public TipoComprobante TipoComprobante { get; set; } = TipoComprobante.TicketInterno;
        public string? RncCliente { get; set; }
        public string? RazonSocialCliente { get; set; }
        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "La venta debe contener al menos un producto.")]
        [MinLength(1, ErrorMessage = "El carrito no puede estar vacío.")]
        public List<ItemCarritoVentaDto> Items { get; set; } = new();

        [Required(ErrorMessage = "Debe registrar los pagos de la venta.")]
        [MinLength(1, ErrorMessage = "Debe ingresar al menos un método de pago.")]
        public List<PagoItemDto> Pagos { get; set; } = new();
    }

    public class ItemCarritoVentaDto
    {
        [Required]
        public Guid ProductoVarianteId { get; set; }

        [Range(1, 1000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Cantidad { get; set; }

        [Range(0, 100, ErrorMessage = "El porcentaje de descuento debe estar entre 0 y 100.")]
        public decimal PorcentajeDescuento { get; set; } = 0m;
    }

    public class PagoItemDto
    {
        public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

        [Range(0.01, 9999999, ErrorMessage = "El monto a pagar debe ser mayor a 0.")]
        public decimal Monto { get; set; }

        public string? Referencia { get; set; }
    }
}
