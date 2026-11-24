using DC_BOUTIQUE.Core.Application.DTOs.Account;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace DC_BOUTIQUE.Infrastructure.Identity.Services
{
    public class AccountServiceWebApp : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountServiceWebApp(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request)
        {
            var response = new AuthenticationResponse();
            var user = await _userManager.FindByNameAsync(request.UserName);

            if (user == null)
            {
                response.HasError = true;
                response.Error = $"No existe una cuenta registrada con el usuario '{request.UserName}'.";
                return response;
            }

            if (!user.IsActive)
            {
                response.HasError = true;
                response.Error = $"La cuenta de '{request.UserName}' está deshabilitada.";
                return response;
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = "Contraseña o credenciales incorrectas.";
                return response;
            }

            response.Id = user.Id;
            response.UserName = user.UserName ?? string.Empty;
            response.FirstName = user.FirstName;
            response.LastName = user.LastName;

            var rolesList = await _userManager.GetRolesAsync(user);
            response.Roles = rolesList.ToList();
            response.HasError = false;

            return response;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<RegisterResponse> RegisterBasicUserAsync(RegisterRequest request)
        {
            var response = new RegisterResponse();

            var userWithSameUserName = await _userManager.FindByNameAsync(request.UserName);
            if (userWithSameUserName != null)
            {
                response.HasError = true;
                response.Error = $"El nombre de usuario '{request.UserName}' ya está en uso.";
                return response;
            }

            var user = new AppUser
            {
                UserName = request.UserName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Cedula = request.Cedula,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = string.Join(", ", result.Errors.Select(e => e.Description));
                return response;
            }

            string roleToAssign = string.IsNullOrWhiteSpace(request.Role) ? "Vendedor" : request.Role;
            await _userManager.AddToRoleAsync(user, roleToAssign);

            response.HasError = false;
            return response;
        }
    }
}
