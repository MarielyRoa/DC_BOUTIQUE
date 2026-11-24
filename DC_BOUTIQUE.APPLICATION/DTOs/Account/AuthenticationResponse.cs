namespace DC_BOUTIQUE.Core.Application.DTOs.Account
{
    public class AuthenticationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
