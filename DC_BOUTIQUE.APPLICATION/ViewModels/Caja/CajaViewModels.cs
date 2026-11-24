using DC_BOUTIQUE.Core.Application.ViewModels.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.ViewModels.Caja
{
    public class TurnoCajaViewModel : BasicViewModel
    {
        public string CodigoTurno { get; set; } = string.Empty;
        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;

        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }

        public decimal FondoInicialEfectivo { get; set; }
        public decimal? EfectivoContadoAlCierre { get; set; }
        public decimal? DiferenciaArqueo { get; set; }

        public EstadoCaja Estado { get; set; }
        public string? Observaciones { get; set; }

        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasTarjeta { get; set; }
        public decimal TotalVentasTransferencia { get; set; }
        public decimal TotalGastosEfectivo { get; set; }
        public decimal EfectivoEsperadoEnCaja => FondoInicialEfectivo + TotalVentasEfectivo - TotalGastosEfectivo;
    }

    public class AbrirTurnoViewModel
    {
        [Required(ErrorMessage = "El fondo inicial es obligatorio.")]
        [Range(0, 9999999, ErrorMessage = "El fondo inicial debe ser mayor o igual a 0.")]
        public decimal FondoInicialEfectivo { get; set; }

        public string? Observaciones { get; set; }
    }

    public class CerrarTurnoViewModel
    {
        public Guid TurnoId { get; set; }

        [Required(ErrorMessage = "Debe ingresar el efectivo contado en caja.")]
        [Range(0, 9999999, ErrorMessage = "El monto debe ser mayor o igual a 0.")]
        public decimal EfectivoContadoAlCierre { get; set; }

        public string? Observaciones { get; set; }
    }

    public class GastoCajaViewModel : BasicViewModel
    {
        public Guid TurnoCajaId { get; set; }
        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string CategoriaGasto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public string? ComprobanteReferencia { get; set; }
        public string? Observaciones { get; set; }
    }

    public class SaveGastoCajaViewModel
    {
        public Guid TurnoCajaId { get; set; }

        [Required(ErrorMessage = "El concepto del gasto es obligatorio.")]
        [StringLength(200)]
        public string Concepto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría del gasto es obligatoria.")]
        [StringLength(80)]
        public string CategoriaGasto { get; set; } = "Insumos Boutique";

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Range(0.01, 9999999, ErrorMessage = "El monto del gasto debe ser mayor a 0.")]
        public decimal Monto { get; set; }

        public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

        [StringLength(80)]
        public string? ComprobanteReferencia { get; set; }

        [StringLength(300)]
        public string? Observaciones { get; set; }
    }
}
