using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Caja
{
    public class AbrirTurnoDto
    {
        [Required(ErrorMessage = "El fondo inicial es obligatorio.")]
        [Range(0, 9999999, ErrorMessage = "El fondo inicial debe ser mayor o igual a 0.")]
        public decimal FondoInicialEfectivo { get; set; }

        public string? Observaciones { get; set; }
    }

    public class CerrarTurnoDto
    {
        public Guid TurnoId { get; set; }

        [Required(ErrorMessage = "Debe ingresar el efectivo contado físico.")]
        [Range(0, 9999999, ErrorMessage = "El monto contado debe ser mayor o igual a 0.")]
        public decimal EfectivoContadoAlCierre { get; set; }

        public string? Observaciones { get; set; }
    }
}
