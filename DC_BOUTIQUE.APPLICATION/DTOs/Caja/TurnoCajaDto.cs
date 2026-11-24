using DC_BOUTIQUE.Core.Application.DTOs.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Application.DTOs.Caja
{
    public class TurnoCajaDto : BasicDto
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

        // Totales calculados para la pantalla de arqueo
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasTarjeta { get; set; }
        public decimal TotalVentasTransferencia { get; set; }
        public decimal TotalGastosEfectivo { get; set; }
        public decimal EfectivoEsperadoEnCaja => FondoInicialEfectivo + TotalVentasEfectivo - TotalGastosEfectivo;
    }
}
