using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IClienteRepository : IGenericRepository<Cliente>
    {
        Task<Cliente?> GetByTelefonoAsync(string telefono, CancellationToken cancellationToken = default);
        Task<Cliente?> GetByCedulaORncAsync(string cedulaORnc, CancellationToken cancellationToken = default);
        Task<List<Cliente>> BuscarAsync(string filtro, CancellationToken cancellationToken = default);
    }
}
