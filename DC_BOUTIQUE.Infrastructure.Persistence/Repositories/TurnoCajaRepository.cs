using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class TurnoCajaRepository : GenericRepository<TurnoCaja>, ITurnoCajaRepository
    {
        public TurnoCajaRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<TurnoCaja?> GetTurnoAbiertoAsync(Guid? usuarioId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(t => t.Usuario)
                .Where(t => t.Estado == EstadoCaja.Abierta && t.Activo);

            if (usuarioId.HasValue && usuarioId.Value != Guid.Empty)
            {
                var delUsuario = await query.FirstOrDefaultAsync(t => t.UsuarioId == usuarioId.Value, cancellationToken);
                if (delUsuario != null) return delUsuario;
            }

            // Si el turno lo abrió la administradora u otro cajero en el mostrador, compartir el turno abierto
            return await query.OrderByDescending(t => t.FechaApertura).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TurnoCaja?> GetUltimoTurnoAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Usuario)
                .OrderByDescending(t => t.FechaApertura)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> HayTurnoAbiertoAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(t => t.Estado == EstadoCaja.Abierta && t.Activo, cancellationToken);
        }
    }
}
