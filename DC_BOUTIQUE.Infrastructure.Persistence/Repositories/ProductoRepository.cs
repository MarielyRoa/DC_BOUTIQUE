using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class ProductoRepository : GenericRepository<Producto>, IProductoRepository
    {
        public ProductoRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<Producto?> GetDetalleCompletoAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.Variantes.Where(v => v.Activo))
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<List<Producto>> GetPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.CategoriaId == categoriaId && p.Activo)
                .Include(p => p.Variantes.Where(v => v.Activo))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExisteCodigoModeloAsync(string codigoModelo, Guid? excluirId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(p => p.CodigoModelo.ToLower() == codigoModelo.Trim().ToLower());
            if (excluirId.HasValue)
            {
                query = query.Where(p => p.Id != excluirId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
