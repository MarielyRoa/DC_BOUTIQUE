using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Clientes
{
    public class SaveClienteDto
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CedulaORnc { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(25)]
        public string Telefono { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(120)]
        public string? Correo { get; set; }

        [StringLength(250)]
        public string? Direccion { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }
    }
}
