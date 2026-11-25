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
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountServiceWebApp(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
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

            string roleToAssign = string.IsNullOrWhiteSpace(request.Role) ? "Cajero" : request.Role.Trim();

            // Normalizar alias de dominio ("Vendedor") al rol oficial de Identity ("Cajero")
            if (roleToAssign.Equals("Vendedor", StringComparison.OrdinalIgnoreCase))
            {
                roleToAssign = "Cajero";
            }

            // Seguridad: nunca permitir asignar el rol Desarrollador desde el flujo ordinario de usuarios de boutique
            if (roleToAssign.Equals("Desarrollador", StringComparison.OrdinalIgnoreCase))
            {
                response.HasError = true;
                response.Error = "No está permitido asignar el rol Desarrollador desde la gestión de usuarios.";
                return response;
            }

            await _userManager.AddToRoleAsync(user, roleToAssign);

            response.HasError = false;
            return response;
        }

        public async Task<RegisterResponse> RegisterDeveloperInitialAsync(RegisterRequest request)
        {
            var response = new RegisterResponse();

            if (!await _roleManager.RoleExistsAsync("Desarrollador"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Desarrollador"));
            }

            var existingDevs = await _userManager.GetUsersInRoleAsync("Desarrollador");
            if (existingDevs.Any())
            {
                response.HasError = true;
                response.Error = "Ya existe una cuenta con rol Desarrollador configurada en Identity.";
                return response;
            }

            var userWithSameUserName = await _userManager.FindByNameAsync(request.UserName.Trim());
            if (userWithSameUserName != null)
            {
                response.HasError = true;
                response.Error = $"El nombre de usuario '{request.UserName}' ya está en uso.";
                return response;
            }

            var user = new AppUser
            {
                UserName = request.UserName.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
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

            await _userManager.AddToRoleAsync(user, "Desarrollador");

            response.HasError = false;
            return response;
        }

        public Task<Dictionary<string, string?>> GetUserEmailsByUserNameAsync()
        {
            var map = _userManager.Users
                .Where(u => u.UserName != null)
                .ToList()
                .GroupBy(u => u.UserName!.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First().Email);

            return Task.FromResult(map);
        }

        public async Task<(bool Success, string Message)> ResetPasswordByTechAdminAsync(string userName, string newPassword)
        {
            var user = await _userManager.FindByNameAsync(userName.Trim());
            if (user == null)
            {
                return (true, "Usuario no presente en Identity; actualizado en dominio.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var res = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!res.Succeeded)
            {
                return (false, string.Join("; ", res.Errors.Select(e => e.Description)));
            }

            return (true, "Contraseña actualizada en ASP.NET Core Identity.");
        }

        public async Task<(bool Success, string Message)> SetActiveStateByTechAdminAsync(string userName, bool isActive)
        {
            var user = await _userManager.FindByNameAsync(userName.Trim());
            if (user == null)
            {
                return (true, "Usuario sincronizado en dominio.");
            }

            user.IsActive = isActive;
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                return (false, string.Join("; ", res.Errors.Select(e => e.Description)));
            }

            return (true, "Estado sincronizado en ASP.NET Core Identity.");
        }
    }
}
