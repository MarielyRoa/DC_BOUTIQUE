using DC_BOUTIQUE.Core.Application.DTOs.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Caja
{
    public class GastoCajaDto : BasicDto
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

    public class SaveGastoCajaDto
    {
        public Guid TurnoCajaId { get; set; }

        [Required(ErrorMessage = "El concepto del gasto es obligatorio.")]
        [StringLength(200)]
        public string Concepto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría del gasto es obligatoria.")]
        [StringLength(80)]
        public string CategoriaGasto { get; set; } = "Insumos Boutique"; // Empaques, fundas, transporte, limpieza

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
