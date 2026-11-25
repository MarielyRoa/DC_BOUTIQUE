using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.ViewModels.TechAdmin
{
    public class SetupDesarrolladorViewModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(120, MinimumLength = 3, ErrorMessage = "Debe tener entre 3 y 120 caracteres.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario técnico es obligatorio.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Debe tener entre 4 y 50 caracteres.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Solo se permiten letras, números, punto, guion y guion bajo.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña técnica es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña técnica debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
