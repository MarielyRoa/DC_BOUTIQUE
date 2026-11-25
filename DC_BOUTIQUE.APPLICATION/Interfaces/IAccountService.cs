using DC_BOUTIQUE.Core.Application.DTOs.Account;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IAccountService
    {
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request);
        Task SignOutAsync();
        Task<RegisterResponse> RegisterBasicUserAsync(RegisterRequest request);
        Task<RegisterResponse> RegisterDeveloperInitialAsync(RegisterRequest request);
        Task<Dictionary<string, string?>> GetUserEmailsByUserNameAsync();
        Task<(bool Success, string Message)> ResetPasswordByTechAdminAsync(string userName, string newPassword);
        Task<(bool Success, string Message)> SetActiveStateByTechAdminAsync(string userName, bool isActive);
    }
}
