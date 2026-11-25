namespace DC_BOUTIQUE.Core.Application.DTOs.Account
{
    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Cedula { get; set; }
        public string Role { get; set; } = "Cajero";
    }
}
