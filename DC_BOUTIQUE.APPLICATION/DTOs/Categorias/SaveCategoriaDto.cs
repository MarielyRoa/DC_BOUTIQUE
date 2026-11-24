using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Categorias
{
    public class SaveCategoriaDto
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La descripción no puede exceder 300 caracteres.")]
        public string? Descripcion { get; set; }
    }
}
