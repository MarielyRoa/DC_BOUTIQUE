using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class VentaRepository : GenericRepository<Venta>, IVentaRepository
    {
        public VentaRepository(DCBoutiqueContext context) : base(context) { }

        public async Task<Venta?> GetDetalleCompletoAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Detalles)
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<Venta?> GetByNumeroFacturaAsync(string numeroFactura, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Detalles)
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.NumeroFactura == numeroFactura.Trim(), cancellationToken);
        }

        public async Task<List<Venta>> GetPorTurnoAsync(Guid turnoCajaId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(v => v.TurnoCajaId == turnoCajaId && v.Activo)
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Pagos)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Venta>> GetVentasDelDiaAsync(DateTime fecha, CancellationToken cancellationToken = default)
        {
            var inicio = fecha.Date;
            var fin = inicio.AddDays(1);
            return await _dbSet
                .Where(v => v.FechaVenta >= inicio && v.FechaVenta < fin && v.Activo)
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Pagos)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync(cancellationToken);
        }
    }
}
