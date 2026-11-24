using System.Text.Json;
using DC_BOUTIQUE.Core.Application.ViewModels.Usuarios;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DC_BOUTIQUE.WEBAPP.Middlewares
{
    public class AuthorizeAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userJson = context.HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(userJson))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            try
            {
                var user = JsonSerializer.Deserialize<UsuarioViewModel>(userJson);
                if (user == null || user.Rol != RolUsuario.Administrador)
                {
                    if (context.Controller is Controller controller)
                    {
                        controller.TempData["ErrorMessage"] = "Acceso restringido: Esta sección requiere privilegios de Administrador.";
                    }
                    context.Result = new RedirectToActionResult("Index", "Pos", null);
                    return;
                }
            }
            catch
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
