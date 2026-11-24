using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class GastoCajaRepository : GenericRepository<GastoCaja>, IGastoCajaRepository
    {
        public GastoCajaRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<List<GastoCaja>> GetPorTurnoAsync(Guid turnoCajaId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(g => g.TurnoCajaId == turnoCajaId && g.Activo)
                .OrderByDescending(g => g.FechaCreacion)
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetTotalGastosPorTurnoAsync(Guid turnoCajaId, CancellationToken cancellationToken = default)
        {
            // Compatible con SQLite: evita SumAsync directo sobre columnas decimal
            var montos = await _dbSet
                .Where(g => g.TurnoCajaId == turnoCajaId && g.Activo)
                .Select(g => g.Monto)
                .ToListAsync(cancellationToken);

            return montos.Sum();
        }
    }
}
