using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DC_BOUTIQUE.WEBAPP.Middlewares
{
    /// <summary>
    /// Protege controladores operativos (POS, Caja, Inicio) exigiendo sesión activa
    /// de cualquier usuario autenticado (Desarrollador, Administrador o Cajero/Vendedor).
    /// </summary>
    public class AuthorizeSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userJson = context.HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(userJson))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
