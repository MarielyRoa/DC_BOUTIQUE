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
        private readonly ITechAdminService _techService;

        public AuthController(
            IAccountService accountService,
            IUsuarioService usuarioService,
            ITechAdminService techService)
        {
            _accountService = accountService;
            _usuarioService = usuarioService;
            _techService = techService;
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            await LoadActiveLoginUsersAsync();
            return View(new LoginViewModel());
        }

        private async Task LoadActiveLoginUsersAsync()
        {
            try
            {
                var usuarios = await _usuarioService.GetAllAsync();
                // Mostrar únicamente personal activo de la boutique (mantener oculto el rol Desarrollador)
                ViewBag.UsuariosDisponibles = usuarios
                    .Where(u => u.Activo && u.Rol != Core.Domain.Common.Enums.RolUsuario.Desarrollador)
                    .OrderByDescending(u => u.EsPrincipal)
                    .ThenBy(u => u.Rol)
                    .ThenBy(u => u.NombreCompleto)
                    .ToList();
            }
            catch
            {
                ViewBag.UsuariosDisponibles = new List<UsuarioDto>();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            await LoadActiveLoginUsersAsync();
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
                        var userExists = await _usuarioService.GetByNombreUsuarioAsync(model.Username.Trim());
                        string motivoFallo;
                        if (userExists == null)
                        {
                            motivoFallo = "Intento de acceso con usuario no registrado.";
                            ModelState.AddModelError("", "Usuario no identificado o no registrado en el sistema.");
                        }
                        else if (!userExists.Activo)
                        {
                            motivoFallo = "Intento de acceso con usuario inactivo.";
                            ModelState.AddModelError("", "Este usuario se encuentra inactivo. Contacte al administrador.");
                        }
                        else
                        {
                            motivoFallo = "Credenciales inválidas (contraseña o PIN incorrecto).";
                            ModelState.AddModelError("", "Contraseña o PIN incorrecto.");
                        }

                        // Registrar fallo de autenticación SIN revelar contraseñas ni credenciales
                        await _techService.LogEventAsync(
                            Core.Domain.Common.Enums.TipoLogTecnico.Autenticacion,
                            "Warning",
                            "AuthController.Login",
                            motivoFallo,
                            $"IP remota: {HttpContext.Connection.RemoteIpAddress}",
                            model.Username.Trim());
                        return View(model);
                    }

                    if (!domainUser.Activo)
                    {
                        ModelState.AddModelError("", "Este usuario se encuentra inactivo. Contacte al administrador.");
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
                    await _techService.RegisterUserLastAccessAsync(domainUser.Id);

                    TempData["SuccessMessage"] = $"¡Bienvenida, {domainUser.NombreCompleto}!";
                    if (domainUser.Rol == Core.Domain.Common.Enums.RolUsuario.Desarrollador)
                    {
                        await _techService.RecordAuditAsync(domainUser.NombreUsuario, "Seguridad", "Inicio de Sesión Técnico", true, "Acceso autenticado al Centro de Administración Técnica.");
                        return RedirectToAction("Index", "TechAdmin");
                    }
                    return domainUser.Rol == Core.Domain.Common.Enums.RolUsuario.Vendedor
                        ? RedirectToAction("Index", "Pos")
                        : RedirectToAction("Index", "Admin");
                }

                // Autenticación exitosa en Identity — validar estado real en tabla de Dominio dbo.Usuarios
                var domainUserRecord = await _usuarioService.GetByNombreUsuarioAsync(authResponse.UserName);
                if (domainUserRecord == null)
                {
                    // Si el usuario fue eliminado permanentemente del módulo Usuarios & Permisos, no permitir acceso ni recrearlo
                    ModelState.AddModelError("", "Usuario no identificado o eliminado del sistema.");
                    return View(model);
                }

                if (!domainUserRecord.Activo)
                {
                    ModelState.AddModelError("", "Este usuario se encuentra inactivo. Contacte al administrador.");
                    return View(model);
                }

                var activeRole = domainUserRecord.Rol;
                Guid finalUserId = domainUserRecord.Id;

                var identityUserVm = new UsuarioViewModel
                {
                    Id = finalUserId,
                    NombreCompleto = !string.IsNullOrWhiteSpace(domainUserRecord.NombreCompleto)
                        ? domainUserRecord.NombreCompleto
                        : $"{authResponse.FirstName} {authResponse.LastName}".Trim(),
                    NombreUsuario = domainUserRecord.NombreUsuario,
                    Rol = activeRole,
                    Cedula = domainUserRecord.Cedula,
                    Telefono = domainUserRecord.Telefono,
                    PorcentajeComision = domainUserRecord.PorcentajeComision,
                    Activo = domainUserRecord.Activo,
                    FechaCreacion = domainUserRecord.FechaCreacion
                };

                HttpContext.Session.SetString("user", JsonSerializer.Serialize(identityUserVm));
                HttpContext.Session.SetString("UserId", identityUserVm.Id.ToString());
                HttpContext.Session.SetString("UserName", identityUserVm.NombreUsuario);
                HttpContext.Session.SetString("UserRole", activeRole.ToString());
                await _techService.RegisterUserLastAccessAsync(identityUserVm.Id);

                TempData["SuccessMessage"] = $"¡Bienvenida, {identityUserVm.NombreCompleto}!";

                if (activeRole == Core.Domain.Common.Enums.RolUsuario.Desarrollador)
                {
                    await _techService.RecordAuditAsync(identityUserVm.NombreUsuario, "Seguridad", "Inicio de Sesión Técnico", true, "Acceso autenticado vía Identity al Centro de Administración Técnica.");
                    return RedirectToAction("Index", "TechAdmin");
                }

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
