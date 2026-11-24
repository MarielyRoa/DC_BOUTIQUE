using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class CategoriaRepository : GenericRepository<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(c => c.Nombre.ToLower() == nombre.Trim().ToLower());
            if (excluirId.HasValue)
            {
                query = query.Where(c => c.Id != excluirId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
