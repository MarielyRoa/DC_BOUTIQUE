using DC_BOUTIQUE.Core.Application.Extensions;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Usuarios;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeAdmin]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        public async Task<IActionResult> Index()
        {
            var dtos = await _usuarioService.GetAllAsync();
            // Ocultar la cuenta técnica Desarrollador del listado operativo de empleados de la boutique
            var viewModels = dtos
                .Where(d => d.Rol != Core.Domain.Common.Enums.RolUsuario.Desarrollador)
                .OrderByDescending(d => d.EsPrincipal)
                .ThenBy(d => d.Rol)
                .ThenBy(d => d.NombreCompleto)
                .Select(d => d.ToViewModel())
                .ToList();
            return View(viewModels);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SaveUsuarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveUsuarioViewModel model)
        {
            if (model.Rol == Core.Domain.Common.Enums.RolUsuario.Desarrollador)
            {
                TempData["ErrorMessage"] = "No está permitido asignar el rol Desarrollador desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los siguientes campos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var dto = model.ToDto();
            var result = await _usuarioService.CreateAsync(dto);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var dto = await _usuarioService.GetByIdAsync(id);
            if (dto == null || dto.Rol == Core.Domain.Common.Enums.RolUsuario.Desarrollador)
            {
                TempData["ErrorMessage"] = "El usuario seleccionado no existe o está protegido.";
                return RedirectToAction(nameof(Index));
            }

            var model = dto.ToSaveViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SaveUsuarioViewModel model)
        {
            if (model.Rol == Core.Domain.Common.Enums.RolUsuario.Desarrollador)
            {
                TempData["ErrorMessage"] = "No está permitido asignar el rol Desarrollador desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los campos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var dto = model.ToDto();
            var result = await _usuarioService.UpdateAsync(id, dto);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _usuarioService.DeleteAsync(id);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _usuarioService.HardDeleteAsync(id);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(Guid id)
        {
            var result = await _usuarioService.ActivarAsync(id);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
