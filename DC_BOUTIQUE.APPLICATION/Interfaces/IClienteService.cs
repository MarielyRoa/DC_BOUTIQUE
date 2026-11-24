using DC_BOUTIQUE.Core.Application.DTOs.Clientes;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IClienteService : IGenericService<ClienteDto, SaveClienteDto>
    {
        Task<List<ClienteDto>> BuscarAsync(string filtro, CancellationToken cancellationToken = default);
    }
}
