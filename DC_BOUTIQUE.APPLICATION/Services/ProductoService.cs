using DC_BOUTIQUE.Core.Application.DTOs.Productos;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;

namespace DC_BOUTIQUE.Core.Application.Services
{
    public class ProductoService : IProductoService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductoService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ProductoDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var productos = await _unitOfWork.Productos.GetAllListWithInclude(new List<string> { "Categoria", "Variantes" }, cancellationToken);
            return productos.Where(p => p.Activo).Select(MapToDto).ToList();
        }

        public async Task<ProductoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var p = await _unitOfWork.Productos.GetDetalleCompletoAsync(id, cancellationToken);
            return p == null ? null : MapToDto(p);
        }

        public async Task<List<ProductoDto>> GetPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
        {
            var productos = await _unitOfWork.Productos.GetPorCategoriaAsync(categoriaId, cancellationToken);
            return productos.Where(p => p.Activo).Select(MapToDto).ToList();
        }

        public async Task<ProductoVarianteDto?> GetVarianteBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            var v = await _unitOfWork.ProductoVariantes.GetBySkuAsync(sku, cancellationToken);
            return v == null ? null : MapVarianteToDto(v);
        }

        public async Task<ProductoVarianteDto?> GetVarianteByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
        {
            var v = await _unitOfWork.ProductoVariantes.GetByCodigoBarrasAsync(codigoBarras, cancellationToken);
            return v == null ? null : MapVarianteToDto(v);
        }

        public async Task<List<ProductoVarianteDto>> GetVariantesConStockBajoAsync(CancellationToken cancellationToken = default)
        {
            var variantes = await _unitOfWork.ProductoVariantes.GetConStockBajoAsync(cancellationToken);
            return variantes.Select(MapVarianteToDto).ToList();
        }

        public async Task<Result<ProductoDto>> CreateAsync(SaveProductoDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return Result<ProductoDto>.Failure("El nombre de la prenda es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(dto.CodigoModelo))
            {
                return Result<ProductoDto>.Failure("El código de modelo es obligatorio.");
            }

            if (dto.CategoriaId == Guid.Empty)
            {
                return Result<ProductoDto>.Failure("Debe seleccionar una categoría válida.");
            }

            if (dto.Variantes == null || dto.Variantes.Count == 0)
            {
                return Result<ProductoDto>.Failure("Debe agregar al menos una combinación de talla y color.");
            }

            var modeloLimpio = dto.CodigoModelo.Trim().ToUpper();
            var existeModelo = await _unitOfWork.Productos.ExisteCodigoModeloAsync(modeloLimpio, null, cancellationToken);
            if (existeModelo)
            {
                return Result<ProductoDto>.Failure($"El código de modelo '{modeloLimpio}' ya se encuentra registrado. Utilice un código diferente.");
            }

            // Validar SKUs duplicados en base de datos
            foreach (var v in dto.Variantes)
            {
                if (string.IsNullOrWhiteSpace(v.Sku))
                {
                    return Result<ProductoDto>.Failure("Cada variante debe tener un SKU o código único.");
                }

                var skuExiste = await _unitOfWork.ProductoVariantes.GetBySkuAsync(v.Sku.Trim().ToUpper(), cancellationToken);
                if (skuExiste != null)
                {
                    return Result<ProductoDto>.Failure($"Ya existe una prenda con el SKU '{v.Sku.Trim().ToUpper()}'. Verifique las combinaciones de tallas y colores.");
                }
            }

            var producto = new Producto
            {
                CodigoModelo = modeloLimpio,
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim(),
                Marca = dto.Marca?.Trim(),
                CategoriaId = dto.CategoriaId,
                MaterialPrincipal = dto.MaterialPrincipal?.Trim(),
                ImagenUrl = dto.ImagenUrl?.Trim(),
                CostoBaseReferencia = dto.CostoBaseReferencia,
                PrecioVentaBase = dto.PrecioVentaBase,
                PrecioOferta = dto.PrecioOferta,
                PorcentajeImpuesto = dto.PorcentajeImpuesto,
                AplicaImpuesto = dto.AplicaImpuesto
            };

            await _unitOfWork.Productos.AddAsync(producto, cancellationToken);

            // Agregar variantes iniciales
            foreach (var v in dto.Variantes)
            {
                var variante = new ProductoVariante
                {
                    ProductoId = producto.Id,
                    Sku = v.Sku.Trim().ToUpper(),
                    CodigoBarras = v.CodigoBarras?.Trim(),
                    Talla = v.Talla.Trim().ToUpper(),
                    Color = v.Color.Trim(),
                    StockActual = v.StockActual,
                    StockMinimoAlerta = v.StockMinimoAlerta,
                    ConcurrencyToken = Guid.NewGuid()
                };

                await _unitOfWork.ProductoVariantes.AddAsync(variante, cancellationToken);

                if (v.StockActual > 0)
                {
                    var adminUser = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync("admin", cancellationToken);
                    var auditUserId = adminUser?.Id ?? (await _unitOfWork.Usuarios.GetAllListAsync(cancellationToken)).FirstOrDefault()?.Id;

                    if (!auditUserId.HasValue || auditUserId.Value == Guid.Empty)
                    {
                        var defaultAdmin = new Usuario
                        {
                            Id = Guid.NewGuid(),
                            NombreCompleto = "Administrador Sistema",
                            NombreUsuario = "admin",
                            PasswordHash = "DEFAULT_SYSTEM_HASH",
                            Rol = RolUsuario.Administrador,
                            Activo = true
                        };
                        await _unitOfWork.Usuarios.AddAsync(defaultAdmin, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        auditUserId = defaultAdmin.Id;
                    }

                    await _unitOfWork.MovimientosInventario.AddAsync(new MovimientoInventario
                    {
                        ProductoVarianteId = variante.Id,
                        UsuarioId = auditUserId.Value,
                        TipoMovimiento = TipoMovimientoInventario.EntradaStock,
                        Cantidad = v.StockActual,
                        StockAnterior = 0,
                        StockPosterior = v.StockActual,
                        CostoUnitarioReferencia = dto.CostoBaseReferencia,
                        Motivo = "Inventario inicial al crear variante"
                    }, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ProductoDto>.Success(MapToDto(producto), $"Prenda '{producto.Nombre}' registrada exitosamente.");
        }

        public async Task<Result> UpdateAsync(Guid id, SaveProductoDto dto, CancellationToken cancellationToken = default)
        {
            var p = await _unitOfWork.Productos.GetByIdAsync(id, cancellationToken);
            if (p == null)
            {
                return Result.Failure("La prenda no existe o fue eliminada.");
            }

            var modeloLimpio = dto.CodigoModelo.Trim().ToUpper();
            var modeloExistente = await _unitOfWork.Productos.ExisteCodigoModeloAsync(modeloLimpio, id, cancellationToken);
            if (modeloExistente)
            {
                return Result.Failure($"El código de modelo '{modeloLimpio}' ya está asignado a otra prenda.");
            }

            p.CodigoModelo = modeloLimpio;
            p.Nombre = dto.Nombre.Trim();
            p.Descripcion = dto.Descripcion?.Trim();
            p.Marca = dto.Marca?.Trim();
            p.CategoriaId = dto.CategoriaId;
            p.MaterialPrincipal = dto.MaterialPrincipal?.Trim();
            p.ImagenUrl = dto.ImagenUrl?.Trim();
            p.CostoBaseReferencia = dto.CostoBaseReferencia;
            p.PrecioVentaBase = dto.PrecioVentaBase;
            p.PrecioOferta = dto.PrecioOferta;
            p.PorcentajeImpuesto = dto.PorcentajeImpuesto;
            p.AplicaImpuesto = dto.AplicaImpuesto;

            await _unitOfWork.Productos.UpdateAsync(p, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Prenda actualizada exitosamente.");
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var p = await _unitOfWork.Productos.GetDetalleCompletoAsync(id, cancellationToken);
            if (p == null)
            {
                return Result.Failure("La prenda que desea eliminar no existe o ya fue eliminada.");
            }

            var varianteIds = p.Variantes.Select(v => v.Id).ToList();
            var tieneVentas = _unitOfWork.Ventas.GetAllQuery()
                .SelectMany(v => v.Detalles)
                .Any(d => varianteIds.Contains(d.ProductoVarianteId));

            if (tieneVentas)
            {
                p.Activo = false;
                foreach (var v in p.Variantes)
                {
                    v.Activo = false;
                }
                await _unitOfWork.Productos.UpdateAsync(p, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success($"Prenda '{p.Nombre}' retirada del catálogo.");
            }
            else
            {
                var movimientos = _unitOfWork.MovimientosInventario.GetAllQuery()
                    .Where(m => varianteIds.Contains(m.ProductoVarianteId))
                    .ToList();

                foreach (var m in movimientos)
                {
                    await _unitOfWork.MovimientosInventario.DeleteAsync(m.Id, cancellationToken);
                }

                foreach (var v in p.Variantes)
                {
                    await _unitOfWork.ProductoVariantes.DeleteAsync(v.Id, cancellationToken);
                }

                await _unitOfWork.Productos.DeleteAsync(p.Id, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success($"Prenda '{p.Nombre}' eliminada exitosamente del catálogo.");
            }
        }

        public async Task<Result> AgregarVarianteAsync(SaveProductoVarianteDto dto, CancellationToken cancellationToken = default)
        {
            var skuLimpio = dto.Sku.Trim().ToUpper();
            var skuExiste = await _unitOfWork.ProductoVariantes.GetBySkuAsync(skuLimpio, cancellationToken);
            if (skuExiste != null)
            {
                return Result.Failure($"Ya existe una variante con el SKU '{skuLimpio}'.");
            }

            var variante = new ProductoVariante
            {
                ProductoId = dto.ProductoId,
                Sku = skuLimpio,
                CodigoBarras = dto.CodigoBarras?.Trim(),
                Talla = dto.Talla.Trim().ToUpper(),
                Color = dto.Color.Trim(),
                StockActual = dto.StockActual,
                StockMinimoAlerta = dto.StockMinimoAlerta,
                ConcurrencyToken = Guid.NewGuid()
            };

            await _unitOfWork.ProductoVariantes.AddAsync(variante, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Variante agregada correctamente.");
        }

        public async Task<Result> ActualizarVarianteAsync(Guid id, SaveProductoVarianteDto dto, CancellationToken cancellationToken = default)
        {
            var v = await _unitOfWork.ProductoVariantes.GetByIdAsync(id, cancellationToken);
            if (v == null)
            {
                return Result.Failure("La variante seleccionada no existe.");
            }

            var skuLimpio = dto.Sku.Trim().ToUpper();
            var skuExiste = await _unitOfWork.ProductoVariantes.GetBySkuAsync(skuLimpio, cancellationToken);
            if (skuExiste != null && skuExiste.Id != id)
            {
                return Result.Failure($"El SKU '{skuLimpio}' ya pertenece a otra variante.");
            }

            v.Sku = skuLimpio;
            v.CodigoBarras = dto.CodigoBarras?.Trim();
            v.Talla = dto.Talla.Trim().ToUpper();
            v.Color = dto.Color.Trim();
            v.StockMinimoAlerta = dto.StockMinimoAlerta;
            v.ConcurrencyToken = Guid.NewGuid();

            await _unitOfWork.ProductoVariantes.UpdateAsync(v, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Variante actualizada exitosamente.");
        }

        public async Task<Result> EliminarVarianteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var v = await _unitOfWork.ProductoVariantes.GetByIdAsync(id, cancellationToken);
            if (v == null)
            {
                return Result.Failure("La variante no existe.");
            }

            v.Activo = false;
            await _unitOfWork.ProductoVariantes.UpdateAsync(v, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Variante eliminada exitosamente.");
        }

        private static ProductoDto MapToDto(Producto p)
        {
            return new ProductoDto
            {
                Id = p.Id,
                CodigoModelo = p.CodigoModelo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Marca = p.Marca,
                CategoriaId = p.CategoriaId,
                NombreCategoria = p.Categoria?.Nombre ?? string.Empty,
                MaterialPrincipal = p.MaterialPrincipal,
                ImagenUrl = p.ImagenUrl,
                CostoBaseReferencia = p.CostoBaseReferencia,
                PrecioVentaBase = p.PrecioVentaBase,
                PrecioOferta = p.PrecioOferta,
                PorcentajeImpuesto = p.PorcentajeImpuesto,
                AplicaImpuesto = p.AplicaImpuesto,
                Activo = p.Activo,
                FechaCreacion = p.FechaCreacion,
                Variantes = p.Variantes.Where(v => v.Activo).Select(MapVarianteToDto).ToList()
            };
        }

        private static ProductoVarianteDto MapVarianteToDto(ProductoVariante v)
        {
            return new ProductoVarianteDto
            {
                Id = v.Id,
                ProductoId = v.ProductoId,
                Sku = v.Sku,
                CodigoBarras = v.CodigoBarras,
                Talla = v.Talla,
                Color = v.Color,
                StockActual = v.StockActual,
                StockMinimoAlerta = v.StockMinimoAlerta,
                ConcurrencyToken = v.ConcurrencyToken,
                Activo = v.Activo,
                FechaCreacion = v.FechaCreacion,
                NombreProducto = v.Producto?.Nombre ?? string.Empty,
                CodigoModelo = v.Producto?.CodigoModelo ?? string.Empty,
                PrecioVenta = v.Producto?.PrecioVentaBase ?? 0m,
                PrecioOferta = v.Producto?.PrecioOferta
            };
        }
    }
}
