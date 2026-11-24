using System.Text.Json;
using DC_BOUTIQUE.Core.Application.DTOs.Account;
using DC_BOUTIQUE.Core.Application.DTOs.Usuarios;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Usuarios;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IUsuarioService _usuarioService;

        public AuthController(IAccountService accountService, IUsuarioService usuarioService)
        {
            _accountService = accountService;
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                ModelState.AddModelError("", string.Join(" ", errors));
                return View(model);
            }

            try
            {
                // 1. Autenticación a través de la Capa Identity
                var authResponse = await _accountService.AuthenticateAsync(new AuthenticationRequest
                {
                    UserName = model.Username.Trim(),
                    Password = model.Password
                });

                if (authResponse.HasError)
                {
                    // Fallback de compatibilidad con usuario de dominio
                    var domainUser = await _usuarioService.AutenticarAsync(model.Username.Trim(), model.Password);
                    if (domainUser == null)
                    {
                        ModelState.AddModelError("", authResponse.Error ?? "Usuario o contraseña/PIN incorrecto.");
                        return View(model);
                    }

                    var userVm = new UsuarioViewModel
                    {
                        Id = domainUser.Id,
                        NombreCompleto = domainUser.NombreCompleto,
                        NombreUsuario = domainUser.NombreUsuario,
                        Rol = domainUser.Rol,
                        Cedula = domainUser.Cedula,
                        Telefono = domainUser.Telefono,
                        PorcentajeComision = domainUser.PorcentajeComision,
                        Activo = domainUser.Activo,
                        FechaCreacion = domainUser.FechaCreacion
                    };

                    HttpContext.Session.SetString("user", JsonSerializer.Serialize(userVm));
                    HttpContext.Session.SetString("UserId", domainUser.Id.ToString());
                    HttpContext.Session.SetString("UserName", domainUser.NombreUsuario);
                    HttpContext.Session.SetString("UserRole", domainUser.Rol.ToString());

                    TempData["SuccessMessage"] = $"¡Bienvenida, {domainUser.NombreCompleto}!";
                    return domainUser.Rol == Core.Domain.Common.Enums.RolUsuario.Vendedor
                        ? RedirectToAction("Index", "Pos")
                        : RedirectToAction("Index", "Admin");
                }

                // Autenticación exitosa en Identity
                var roleName = authResponse.Roles.FirstOrDefault() ?? "Vendedor";
                var activeRole = roleName.Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                    ? Core.Domain.Common.Enums.RolUsuario.Administrador
                    : Core.Domain.Common.Enums.RolUsuario.Vendedor;

                // Sincronizar el Id con la tabla de Dominio dbo.Usuarios para integridad de claves foráneas
                var domainUserRecord = await _usuarioService.GetByNombreUsuarioAsync(authResponse.UserName);
                if (domainUserRecord == null)
                {
                    var syncId = Guid.TryParse(authResponse.Id, out Guid parsedId) ? parsedId : Guid.NewGuid();
                    await _usuarioService.CreateAsync(new SaveUsuarioDto
                    {
                        Id = syncId,
                        NombreCompleto = $"{authResponse.FirstName} {authResponse.LastName}".Trim(),
                        NombreUsuario = authResponse.UserName,
                        Password = model.Password,
                        Rol = activeRole,
                        Cedula = "402-0000000-0"
                    });
                    domainUserRecord = await _usuarioService.GetByNombreUsuarioAsync(authResponse.UserName);
                }

                Guid finalUserId = domainUserRecord?.Id ?? (Guid.TryParse(authResponse.Id, out Guid g) ? g : Guid.NewGuid());

                var identityUserVm = new UsuarioViewModel
                {
                    Id = finalUserId,
                    NombreCompleto = $"{authResponse.FirstName} {authResponse.LastName}".Trim(),
                    NombreUsuario = authResponse.UserName,
                    Rol = activeRole,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                HttpContext.Session.SetString("user", JsonSerializer.Serialize(identityUserVm));
                HttpContext.Session.SetString("UserId", identityUserVm.Id.ToString());
                HttpContext.Session.SetString("UserName", identityUserVm.NombreUsuario);
                HttpContext.Session.SetString("UserRole", activeRole.ToString());

                TempData["SuccessMessage"] = $"¡Bienvenida, {identityUserVm.NombreCompleto}!";

                if (activeRole == Core.Domain.Common.Enums.RolUsuario.Vendedor)
                {
                    return RedirectToAction("Index", "Pos");
                }

                return RedirectToAction("Index", "Admin");
            }
            catch (Exception)
            {
                // Fallback directo con usuario de dominio en caso de fallo en el servicio de Identity
                try
                {
                    var fallbackUser = await _usuarioService.AutenticarAsync(model.Username.Trim(), model.Password);
                    if (fallbackUser != null)
                    {
                        var userVm = new UsuarioViewModel
                        {
                            Id = fallbackUser.Id,
                            NombreCompleto = fallbackUser.NombreCompleto,
                            NombreUsuario = fallbackUser.NombreUsuario,
                            Rol = fallbackUser.Rol,
                            Cedula = fallbackUser.Cedula,
                            Telefono = fallbackUser.Telefono,
                            PorcentajeComision = fallbackUser.PorcentajeComision,
                            Activo = fallbackUser.Activo,
                            FechaCreacion = fallbackUser.FechaCreacion
                        };

                        HttpContext.Session.SetString("user", JsonSerializer.Serialize(userVm));
                        HttpContext.Session.SetString("UserId", fallbackUser.Id.ToString());
                        HttpContext.Session.SetString("UserName", fallbackUser.NombreUsuario);
                        HttpContext.Session.SetString("UserRole", fallbackUser.Rol.ToString());

                        TempData["SuccessMessage"] = $"¡Bienvenida, {fallbackUser.NombreCompleto}!";
                        return fallbackUser.Rol == Core.Domain.Common.Enums.RolUsuario.Vendedor
                            ? RedirectToAction("Index", "Pos")
                            : RedirectToAction("Index", "Admin");
                    }
                }
                catch {}

                ModelState.AddModelError("", "Error al conectar con el servicio de autenticación. Verifique las credenciales.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _accountService.SignOutAsync();
            }
            catch {}
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }
    }
}
