using DC_BOUTIQUE.Core.Application.DTOs.Clientes;
using DC_BOUTIQUE.Core.Application.Extensions;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    public class ClienteController : Controller
    {
        private readonly IClienteService _clienteService;

        public ClienteController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            List<ClienteDto> dtos;
            if (!string.IsNullOrWhiteSpace(search))
            {
                dtos = await _clienteService.BuscarAsync(search);
            }
            else
            {
                dtos = await _clienteService.GetAllAsync();
            }

            var viewModels = dtos.Select(d => d.ToViewModel()).ToList();
            ViewBag.SearchTerm = search;
            return View(viewModels);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SaveClienteViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveClienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los siguientes datos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var dto = model.ToDto();
            var result = await _clienteService.CreateAsync(dto);

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
            var dto = await _clienteService.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var model = dto.ToSaveViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SaveClienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los siguientes datos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var dto = model.ToDto();
            var result = await _clienteService.UpdateAsync(id, dto);

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
            var result = await _clienteService.DeleteAsync(id);
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
