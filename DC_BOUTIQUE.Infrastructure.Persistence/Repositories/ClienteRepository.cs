using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class ClienteRepository : GenericRepository<Cliente>, IClienteRepository
    {
        public ClienteRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<Cliente?> GetByTelefonoAsync(string telefono, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.Telefono == telefono.Trim() && c.Activo, cancellationToken);
        }

        public async Task<Cliente?> GetByCedulaORncAsync(string cedulaORnc, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.CedulaORnc == cedulaORnc.Trim() && c.Activo, cancellationToken);
        }

        public async Task<List<Cliente>> BuscarAsync(string filtro, CancellationToken cancellationToken = default)
        {
            var f = filtro.Trim().ToLower();
            return await _dbSet
                .Where(c => c.Activo && (
                    c.Nombres.ToLower().Contains(f) ||
                    c.Apellidos.ToLower().Contains(f) ||
                    c.Telefono.Contains(f) ||
                    (c.CedulaORnc != null && c.CedulaORnc.Contains(f))))
                .Take(20)
                .ToListAsync(cancellationToken);
        }
    }
}
