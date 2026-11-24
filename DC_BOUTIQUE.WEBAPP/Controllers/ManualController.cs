using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeSession]
    public class ManualController : Controller
    {
        private readonly ValidateUserSession _userSession;

        public ManualController(ValidateUserSession userSession)
        {
            _userSession = userSession;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var user = _userSession.GetUser();
            ViewBag.EsAdmin = user == null || user.Rol == RolUsuario.Administrador;
            ViewBag.NombreUsuario = user?.NombreCompleto ?? "Usuario";
            ViewBag.RutaBaseDatos = DatabasePathHelper.GetDatabaseFilePath();
            ViewBag.RutaBackups = DatabasePathHelper.GetBackupDirectory();
            ViewBag.RutaImagenes = DatabasePathHelper.GetImagesDirectory("productos");

            return View();
        }
    }
}
