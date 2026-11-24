using DC_BOUTIQUE.Core.Application.DTOs.Account;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IAccountService
    {
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request);
        Task SignOutAsync();
        Task<RegisterResponse> RegisterBasicUserAsync(RegisterRequest request);
    }
}
