using System.Net;
using System.Text.Json;

namespace DC_BOUTIQUE.WEBAPP.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate _next, ILogger<GlobalExceptionMiddleware> logger)
        {
            this._next = _next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción global interceptada por GlobalExceptionMiddleware: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            var isAjaxOrApi = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || context.Request.Headers["Accept"].ToString().Contains("application/json")
                || context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.Value?.Contains("ProcesarVenta", StringComparison.OrdinalIgnoreCase) == true;

            if (isAjaxOrApi)
            {
                context.Response.Clear();
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var responsePayload = new
                {
                    success = false,
                    message = "Ocurrió un error inesperado al procesar la solicitud. Por favor intente nuevamente o contacte al administrador.",
                    error = exception.Message
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(responsePayload));
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
