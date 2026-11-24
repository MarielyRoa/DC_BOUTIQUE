using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class MovimientoInventarioRepository : GenericRepository<MovimientoInventario>, IMovimientoInventarioRepository
    {
        public MovimientoInventarioRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<List<MovimientoInventario>> GetPorVarianteAsync(Guid productoVarianteId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.ProductoVarianteId == productoVarianteId)
                .Include(m => m.Usuario)
                .OrderByDescending(m => m.FechaCreacion)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<MovimientoInventario>> GetHistorialRecienteAsync(int cantidad = 50, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.ProductoVariante)
                    .ThenInclude(v => v!.Producto)
                .Include(m => m.Usuario)
                .OrderByDescending(m => m.FechaCreacion)
                .Take(cantidad)
                .ToListAsync(cancellationToken);
        }
    }
}
