using System.Text.Json;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Usuarios;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DC_BOUTIQUE.WEBAPP.Middlewares
{
    /// <summary>
    /// Autorización estricta de servidor para el Centro de Administración Técnica.
    /// Verifica sesión, rol Desarrollador y vigencia real en la base de datos en cada solicitud.
    /// </summary>
    public class AuthorizeDeveloperAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userJson = context.HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(userJson))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            try
            {
                var sessionUser = JsonSerializer.Deserialize<UsuarioViewModel>(userJson);
                if (sessionUser == null || sessionUser.Rol != RolUsuario.Desarrollador)
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }

                // Doble verificación contra base de datos (no confiar únicamente en la cookie de sesión)
                var usuarioService = context.HttpContext.RequestServices.GetService<IUsuarioService>();
                if (usuarioService != null)
                {
                    var dbUser = await usuarioService.GetByIdAsync(sessionUser.Id);
                    if (dbUser == null || !dbUser.Activo || dbUser.Rol != RolUsuario.Desarrollador)
                    {
                        context.HttpContext.Session.Clear();
                        context.Result = new RedirectToActionResult("Login", "Auth", null);
                        return;
                    }
                }
            }
            catch
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            await next();
        }
    }
}
