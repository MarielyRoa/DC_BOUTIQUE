using DC_BOUTIQUE.Core.Application.Extensions;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Categorias;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeAdmin]
    public class CategoriaController : Controller
    {
        private readonly ICategoriaService _categoriaService;

        public CategoriaController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService;
        }

        public async Task<IActionResult> Index()
        {
            var dtos = await _categoriaService.GetAllAsync();
            var viewModels = dtos.Select(d => d.ToViewModel()).ToList();
            return View(viewModels);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SaveCategoriaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveCategoriaViewModel model)
        {
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
            var result = await _categoriaService.CreateAsync(dto);

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
            var dto = await _categoriaService.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["ErrorMessage"] = "Categoría no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            var model = dto.ToSaveViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SaveCategoriaViewModel model)
        {
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
            var result = await _categoriaService.UpdateAsync(id, dto);

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
            var result = await _categoriaService.DeleteAsync(id);
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
