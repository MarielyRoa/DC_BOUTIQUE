using DC_BOUTIQUE.Core.Application.DTOs.Categorias;
using DC_BOUTIQUE.Core.Application.DTOs.Productos;
using DC_BOUTIQUE.Core.Application.Extensions;
using DC_BOUTIQUE.Core.Application.Helpers;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Productos;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeAdmin]
    public class ProductoController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;

        public ProductoController(IProductoService productoService, ICategoriaService categoriaService)
        {
            _productoService = productoService;
            _categoriaService = categoriaService;
        }

        public async Task<IActionResult> Index(Guid? categoriaId)
        {
            await LoadCategoriasDropdown();

            List<ProductoDto> dtos;
            if (categoriaId.HasValue && categoriaId.Value != Guid.Empty)
            {
                dtos = await _productoService.GetPorCategoriaAsync(categoriaId.Value);
            }
            else
            {
                dtos = await _productoService.GetAllAsync();
            }

            var viewModels = dtos.Select(d => d.ToViewModel()).ToList();
            ViewBag.SelectedCategoriaId = categoriaId;
            return View(viewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VaciarCatalogo([FromServices] DC_BOUTIQUE.Infrastructure.Persistence.Contexts.DCBoutiqueContext context, [FromForm] string claveConfirmacion)
        {
            if (string.IsNullOrWhiteSpace(claveConfirmacion) || claveConfirmacion.Trim() != "1234")
            {
                TempData["ErrorMessage"] = "Clave de seguridad incorrecta. No se realizaron cambios en la base de datos.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var detalles = await context.DetallesVenta.ToListAsync();
                if (detalles.Any()) context.DetallesVenta.RemoveRange(detalles);

                var movimientos = await context.MovimientosInventario.ToListAsync();
                if (movimientos.Any()) context.MovimientosInventario.RemoveRange(movimientos);

                var variantes = await context.ProductoVariantes.ToListAsync();
                if (variantes.Any()) context.ProductoVariantes.RemoveRange(variantes);

                var productos = await context.Productos.ToListAsync();
                if (productos.Any()) context.Productos.RemoveRange(productos);

                var categorias = await context.Categorias.ToListAsync();
                if (categorias.Any()) context.Categorias.RemoveRange(categorias);

                await context.SaveChangesAsync();

                var productosPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "productos");
                if (Directory.Exists(productosPath))
                {
                    foreach (var dir in Directory.GetDirectories(productosPath))
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                }

                TempData["SuccessMessage"] = "Catálogo y categorías vaciados completamente. La base de datos está limpia.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al vaciar el catálogo: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriasDropdown();
            var model = new SaveProductoViewModel
            {
                CodigoModelo = $"DC-{DateTime.Now:yyyyMM}-{new Random().Next(100, 999)}",
                PrecioVentaBase = 0m,
                CostoBaseReferencia = 0m,
                PorcentajeImpuesto = 0m,
                AplicaImpuesto = false
            };

            model.Variantes.Add(new SaveProductoVarianteViewModel
            {
                Talla = "Única",
                Color = "Negro",
                Sku = $"{model.CodigoModelo}-UNI-NEG",
                StockActual = 5,
                StockMinimoAlerta = 2
            });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveProductoViewModel model)
        {
            if (model.Variantes == null || model.Variantes.Count == 0)
            {
                await LoadCategoriasDropdown();
                TempData["ErrorMessage"] = "Debe ingresar al menos una combinación de talla y color para la prenda.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriasDropdown();
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los siguientes campos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var dto = model.ToDto();
            var result = await _productoService.CreateAsync(dto);

            if (!result.Succeeded || result.Data == null)
            {
                await LoadCategoriasDropdown();
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            var created = result.Data;
            if (model.File != null && model.File.Length > 0)
            {
                try
                {
                    string imageUrl = await UploadFileHelper.UploadFileAsync(model.File, created.Id, "productos", isEditMode: false);
                    dto.Id = created.Id;
                    dto.ImagenUrl = imageUrl;
                    await _productoService.UpdateAsync(created.Id, dto);
                }
                catch
                {
                    // No detener la creación si falla la imagen
                }
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var dto = await _productoService.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["ErrorMessage"] = "Prenda no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriasDropdown();
            var model = dto.ToSaveViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SaveProductoViewModel model)
        {
            if (model.Variantes == null || model.Variantes.Count == 0)
            {
                await LoadCategoriasDropdown();
                TempData["ErrorMessage"] = "La prenda debe conservar al menos una variante de talla y color.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriasDropdown();
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los campos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var dto = model.ToDto();
            if (model.File != null && model.File.Length > 0)
            {
                try
                {
                    string imageUrl = await UploadFileHelper.UploadFileAsync(model.File, id, "productos", isEditMode: true);
                    dto.ImagenUrl = imageUrl;
                }
                catch
                {
                    // Conservar imagen previa si falla la carga
                }
            }

            var result = await _productoService.UpdateAsync(id, dto);
            if (!result.Succeeded)
            {
                await LoadCategoriasDropdown();
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
            var result = await _productoService.DeleteAsync(id);
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

        private async Task LoadCategoriasDropdown()
        {
            try
            {
                var categorias = await _categoriaService.GetAllAsync();
                ViewBag.Categorias = new SelectList(categorias.Where(c => c.Activo), "Id", "Nombre");
            }
            catch
            {
                ViewBag.Categorias = new SelectList(new List<CategoriaDto>(), "Id", "Nombre");
            }
        }
    }
}
