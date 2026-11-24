using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class ProductoVarianteRepository : GenericRepository<ProductoVariante>, IProductoVarianteRepository
    {
        public ProductoVarianteRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<ProductoVariante?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Producto)
                .FirstOrDefaultAsync(v => v.Sku.ToLower() == sku.Trim().ToLower() && v.Activo, cancellationToken);
        }

        public async Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Producto)
                .FirstOrDefaultAsync(v => v.CodigoBarras == codigoBarras.Trim() && v.Activo, cancellationToken);
        }

        public async Task<List<ProductoVariante>> GetPorProductoIdAsync(Guid productoId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(v => v.ProductoId == productoId && v.Activo)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ProductoVariante>> GetConStockBajoAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Producto)
                .Where(v => v.Activo && v.StockActual <= v.StockMinimoAlerta)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExisteSkuAsync(string sku, Guid? excluirId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(v => v.Sku.ToLower() == sku.Trim().ToLower());
            if (excluirId.HasValue)
            {
                query = query.Where(v => v.Id != excluirId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
