using DC_BOUTIQUE.Core.Application.DTOs.Inventario;
using DC_BOUTIQUE.Core.Application.Extensions;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Inventario;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeAdmin]
    public class InventarioController : Controller
    {
        private readonly IInventarioService _inventarioService;
        private readonly IUnitOfWork _unitOfWork;

        public InventarioController(IInventarioService inventarioService, IUnitOfWork unitOfWork)
        {
            _inventarioService = inventarioService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> EntradaRapida(Guid? categoriaId)
        {
            await LoadCategoriasDropdown();
            await LoadVariantesDropdown(categoriaId);
            ViewBag.SelectedCategoriaId = categoriaId;
            return View(new EntradaMercanciaRapidaViewModel { Cantidad = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EntradaRapida(EntradaMercanciaRapidaViewModel model)
        {
            if (model.ProductoVarianteId == Guid.Empty)
            {
                ModelState.AddModelError("ProductoVarianteId", "Debe seleccionar una prenda y talla para dar entrada de mercancía.");
            }
            if (model.Cantidad <= 0)
            {
                ModelState.AddModelError("Cantidad", "La cantidad debe ser mayor a 0.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriasDropdown();
                await LoadVariantesDropdown();
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los siguientes datos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var userId = await GetCurrentUserIdAsync();
            var dto = model.ToDto();
            var result = await _inventarioService.RegistrarEntradaRapidaAsync(userId, dto);

            if (!result.Succeeded)
            {
                await LoadCategoriasDropdown();
                await LoadVariantesDropdown();
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Kardex));
        }

        [HttpGet]
        public async Task<IActionResult> Ajuste(Guid? categoriaId)
        {
            await LoadCategoriasDropdown();
            await LoadVariantesDropdown(categoriaId);
            ViewBag.SelectedCategoriaId = categoriaId;
            return View(new AjusteInventarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ajuste(AjusteInventarioViewModel model)
        {
            if (model.ProductoVarianteId == Guid.Empty)
            {
                ModelState.AddModelError("ProductoVarianteId", "Debe seleccionar la prenda y talla que desea ajustar.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriasDropdown();
                await LoadVariantesDropdown();
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los campos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var userId = await GetCurrentUserIdAsync();
            var dto = model.ToDto();
            var result = await _inventarioService.RegistrarAjusteAsync(userId, dto);

            if (!result.Succeeded)
            {
                await LoadCategoriasDropdown();
                await LoadVariantesDropdown();
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Kardex));
        }

        [HttpGet]
        public async Task<IActionResult> Kardex()
        {
            var movimientos = await _inventarioService.GetHistorialRecienteAsync(100);
            var vm = movimientos.Select(m => m.ToViewModel()).ToList();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> HistorialVariante(Guid id)
        {
            var movimientos = await _inventarioService.GetPorVarianteAsync(id);
            var vm = movimientos.Select(m => m.ToViewModel()).ToList();

            var variante = await _unitOfWork.ProductoVariantes.GetByIdAsync(id);
            if (variante != null)
            {
                var producto = await _unitOfWork.Productos.GetByIdAsync(variante.ProductoId);
                ViewBag.VarianteInfo = $"{producto?.Nombre} - Talla: {variante.Talla} | Color: {variante.Color} (SKU: {variante.Sku})";
            }

            return View(vm);
        }

        private async Task LoadCategoriasDropdown()
        {
            try
            {
                var categorias = await _unitOfWork.Categorias.GetAllListAsync();
                ViewBag.Categorias = new SelectList(categorias.Where(c => c.Activo), "Id", "Nombre");
            }
            catch
            {
                ViewBag.Categorias = new SelectList(new List<object>(), "Id", "Nombre");
            }
        }

        private async Task LoadVariantesDropdown(Guid? categoriaId = null)
        {
            try
            {
                var query = _unitOfWork.ProductoVariantes.GetAllQuery()
                    .Include(v => v.Producto)
                    .ThenInclude(p => p!.Categoria)
                    .Where(v => v.Activo && v.Producto != null && v.Producto.Activo && !v.Producto.Nombre.ToLower().Contains("vestido satin"));

                if (categoriaId.HasValue && categoriaId.Value != Guid.Empty)
                {
                    query = query.Where(v => v.Producto!.CategoriaId == categoriaId.Value);
                }

                var variantes = await query
                    .OrderBy(v => v.Producto!.Nombre)
                    .ThenBy(v => v.Talla)
                    .ToListAsync();

                var list = variantes.Select(v => new
                {
                    v.Id,
                    Texto = $"{v.Producto!.Nombre} ({v.Producto.Categoria?.Nombre ?? "General"}) — Talla: {v.Talla} | Color: {v.Color} [Stock: {v.StockActual}]"
                }).ToList();

                ViewBag.Variantes = new SelectList(list, "Id", "Texto");
            }
            catch
            {
                ViewBag.Variantes = new SelectList(new List<object>(), "Id", "Texto");
            }
        }

        private async Task<Guid> GetCurrentUserIdAsync()
        {
            try
            {
                var userName = HttpContext.Session.GetString("UserName");
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    var user = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(userName.Trim());
                    if (user != null) return user.Id;
                }

                var sessionUserId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(sessionUserId) && Guid.TryParse(sessionUserId, out Guid guid))
                {
                    var userExists = await _unitOfWork.Usuarios.GetByIdAsync(guid);
                    if (userExists != null) return guid;
                }

                var admin = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync("admin");
                if (admin != null) return admin.Id;

                var anyUser = await _unitOfWork.Usuarios.GetAllListAsync();
                if (anyUser.Any()) return anyUser.First().Id;

                var defaultAdmin = new DC_BOUTIQUE.Core.Domain.Entities.Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreCompleto = "Administrador Sistema",
                    NombreUsuario = "admin",
                    PasswordHash = "DEFAULT_SYSTEM_HASH",
                    Rol = DC_BOUTIQUE.Core.Domain.Common.Enums.RolUsuario.Administrador,
                    Activo = true
                };
                await _unitOfWork.Usuarios.AddAsync(defaultAdmin);
                await _unitOfWork.SaveChangesAsync();
                return defaultAdmin.Id;
            }
            catch
            {
                return Guid.Empty;
            }
        }
    }
}
