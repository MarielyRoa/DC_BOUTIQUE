using DC_BOUTIQUE.Core.Application.DTOs.Categorias;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;

namespace DC_BOUTIQUE.Core.Application.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoriaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<CategoriaDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var categorias = await _unitOfWork.Categorias.GetAllListWithInclude(new List<string> { "Productos" }, cancellationToken);
            return categorias.Select(c => new CategoriaDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Activo = c.Activo,
                FechaCreacion = c.FechaCreacion,
                CantidadProductos = c.Productos?.Count(p => p.Activo) ?? 0
            }).ToList();
        }

        public async Task<CategoriaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var categorias = await _unitOfWork.Categorias.GetAllListWithInclude(new List<string> { "Productos" }, cancellationToken);
            var c = categorias.FirstOrDefault(cat => cat.Id == id);
            if (c == null) return null;

            return new CategoriaDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Activo = c.Activo,
                FechaCreacion = c.FechaCreacion,
                CantidadProductos = c.Productos?.Count(p => p.Activo) ?? 0
            };
        }

        public async Task<Result<CategoriaDto>> CreateAsync(SaveCategoriaDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return Result<CategoriaDto>.Failure("El nombre de la categoría es obligatorio.");
            }

            var todas = await _unitOfWork.Categorias.GetAllListAsync(cancellationToken);
            if (todas.Any(c => c.Nombre.Equals(dto.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Result<CategoriaDto>.Failure($"Ya existe una categoría llamada '{dto.Nombre.Trim()}'.");
            }

            var categoria = new Categoria
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim()
            };

            await _unitOfWork.Categorias.AddAsync(categoria, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = new CategoriaDto
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activo = categoria.Activo,
                FechaCreacion = categoria.FechaCreacion
            };

            return Result<CategoriaDto>.Success(resultDto, "Categoría creada exitosamente.");
        }

        public async Task<Result> UpdateAsync(Guid id, SaveCategoriaDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return Result.Failure("El nombre de la categoría es obligatorio.");
            }

            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id, cancellationToken);
            if (categoria == null)
            {
                return Result.Failure("La categoría que intenta actualizar no existe o fue eliminada.");
            }

            var todas = await _unitOfWork.Categorias.GetAllListAsync(cancellationToken);
            if (todas.Any(c => c.Id != id && c.Nombre.Equals(dto.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure($"Ya existe otra categoría con el nombre '{dto.Nombre.Trim()}'.");
            }

            categoria.Nombre = dto.Nombre.Trim();
            categoria.Descripcion = dto.Descripcion?.Trim();

            await _unitOfWork.Categorias.UpdateAsync(categoria, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Categoría actualizada correctamente.");
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id, cancellationToken);
            if (categoria == null)
            {
                return Result.Failure("La categoría no existe o ya fue eliminada.");
            }

            var tieneProductos = _unitOfWork.Productos.GetAllQuery()
                .Any(p => p.CategoriaId == id);

            if (tieneProductos)
            {
                return Result.Failure($"No se puede eliminar la categoría '{categoria.Nombre}' porque contiene prendas asociadas en el catálogo. Reasigne o elimine las prendas primero.");
            }

            await _unitOfWork.Categorias.HardDeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success($"Categoría '{categoria.Nombre}' eliminada permanentemente de la base de datos.");
        }
    }
}
