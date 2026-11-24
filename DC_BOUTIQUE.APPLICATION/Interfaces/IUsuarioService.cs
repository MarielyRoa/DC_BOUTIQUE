using DC_BOUTIQUE.Core.Application.DTOs.Usuarios;
using DC_BOUTIQUE.Core.Application.Wrappers;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IUsuarioService : IGenericService<UsuarioDto, SaveUsuarioDto>
    {
        Task<UsuarioDto?> AutenticarAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default);
        Task<UsuarioDto?> GetByNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);
        Task<Result> CambiarPasswordAsync(Guid usuarioId, string passwordActual, string nuevoPassword, CancellationToken cancellationToken = default);
        Task<Result> ActivarAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result> HardDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
