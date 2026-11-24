using DC_BOUTIQUE.Core.Application.ViewModels.Common;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.Equals(role, "Vendedor", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Pos");
            }
            return RedirectToAction("Index", "Admin");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
