using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class UsuarioRepository : GenericRepository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.Trim().ToLower() && u.Activo, cancellationToken);
        }

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, Guid? excluirId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(u => u.NombreUsuario.ToLower() == nombreUsuario.Trim().ToLower());
            if (excluirId.HasValue)
            {
                query = query.Where(u => u.Id != excluirId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
