using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IVentaRepository : IGenericRepository<Venta>
    {
        Task<Venta?> GetDetalleCompletoAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Venta?> GetByNumeroFacturaAsync(string numeroFactura, CancellationToken cancellationToken = default);
        Task<List<Venta>> GetPorTurnoAsync(Guid turnoCajaId, CancellationToken cancellationToken = default);
        Task<List<Venta>> GetVentasDelDiaAsync(DateTime fecha, CancellationToken cancellationToken = default);
    }
}
