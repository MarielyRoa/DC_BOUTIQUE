using System.Text.Json;
using DC_BOUTIQUE.Core.Application.ViewModels.Usuarios;

namespace DC_BOUTIQUE.WEBAPP.Middlewares
{
    public class ValidateUserSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ValidateUserSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool HasUser()
        {
            var userJson = _httpContextAccessor.HttpContext?.Session.GetString("user");
            return !string.IsNullOrEmpty(userJson);
        }

        public UsuarioViewModel? GetUser()
        {
            var userJson = _httpContextAccessor.HttpContext?.Session.GetString("user");
            if (string.IsNullOrEmpty(userJson)) return null;

            try
            {
                return JsonSerializer.Deserialize<UsuarioViewModel>(userJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
