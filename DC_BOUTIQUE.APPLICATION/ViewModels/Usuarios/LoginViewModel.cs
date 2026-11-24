using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.ViewModels.Usuarios
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario o código es obligatorio.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña o PIN es obligatorio.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = true;

        public string AccessModule { get; set; } = "POS";
    }
}
