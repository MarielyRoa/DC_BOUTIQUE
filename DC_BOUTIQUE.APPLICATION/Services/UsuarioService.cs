using DC_BOUTIQUE.Core.Application.DTOs.Usuarios;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace DC_BOUTIQUE.Core.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsuarioService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<UsuarioDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var usuarios = await _unitOfWork.Usuarios.GetAllListAsync(cancellationToken);
            return usuarios.Select(MapToDto).ToList();
        }

        public async Task<UsuarioDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken);
            return u == null ? null : MapToDto(u);
        }

        public async Task<UsuarioDto?> GetByNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(nombreUsuario, cancellationToken);
            return u == null ? null : MapToDto(u);
        }

        public async Task<UsuarioDto?> AutenticarAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(nombreUsuario, cancellationToken);
            if (u == null) return null;

            var inputHash = HashPassword(password);
            if (u.PasswordHash != inputHash) return null;

            // Actualizar último acceso
            u.UltimoAcceso = DateTime.UtcNow;
            await _unitOfWork.Usuarios.UpdateAsync(u, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToDto(u);
        }

        public async Task<Result<UsuarioDto>> CreateAsync(SaveUsuarioDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreUsuario))
            {
                return Result<UsuarioDto>.Failure("El nombre de usuario es obligatorio.");
            }

            var existe = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(dto.NombreUsuario.Trim().ToLower(), cancellationToken);
            if (existe != null)
            {
                return Result<UsuarioDto>.Failure($"El nombre de usuario '{dto.NombreUsuario.Trim()}' ya está en uso.");
            }

            var pwd = string.IsNullOrWhiteSpace(dto.Password) ? "123456" : dto.Password;
            var usuario = new Usuario
            {
                Id = dto.Id.HasValue && dto.Id.Value != Guid.Empty ? dto.Id.Value : Guid.NewGuid(),
                NombreCompleto = dto.NombreCompleto.Trim(),
                NombreUsuario = dto.NombreUsuario.Trim().ToLower(),
                PasswordHash = HashPassword(pwd),
                Cedula = dto.Cedula?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Rol = dto.Rol,
                PorcentajeComision = dto.PorcentajeComision,
                Activo = true
            };

            await _unitOfWork.Usuarios.AddAsync(usuario, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UsuarioDto>.Success(MapToDto(usuario), $"Usuario '{usuario.NombreUsuario}' creado correctamente.");
        }

        public async Task<Result> UpdateAsync(Guid id, SaveUsuarioDto dto, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken);
            if (u == null)
            {
                return Result.Failure("El usuario no existe o ya fue eliminado.");
            }

            var usuarioExistente = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(dto.NombreUsuario.Trim().ToLower(), cancellationToken);
            if (usuarioExistente != null && usuarioExistente.Id != id)
            {
                return Result.Failure($"El nombre de usuario '{dto.NombreUsuario.Trim()}' ya está registrado por otro colaborador.");
            }

            u.NombreCompleto = dto.NombreCompleto.Trim();
            u.NombreUsuario = dto.NombreUsuario.Trim().ToLower();
            u.Cedula = dto.Cedula?.Trim();
            u.Telefono = dto.Telefono?.Trim();
            u.Rol = dto.Rol;
            u.PorcentajeComision = dto.PorcentajeComision;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                u.PasswordHash = HashPassword(dto.Password);
            }

            await _unitOfWork.Usuarios.UpdateAsync(u, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Usuario actualizado exitosamente.");
        }

        public async Task<Result> CambiarPasswordAsync(Guid usuarioId, string passwordActual, string nuevoPassword, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId, cancellationToken);
            if (u == null)
            {
                return Result.Failure("Usuario no encontrado.");
            }

            if (u.PasswordHash != HashPassword(passwordActual))
            {
                return Result.Failure("La contraseña actual proporcionada es incorrecta.");
            }

            u.PasswordHash = HashPassword(nuevoPassword);
            await _unitOfWork.Usuarios.UpdateAsync(u, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Contraseña cambiada exitosamente.");
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken);
            if (u == null)
            {
                return Result.Failure("El usuario no existe.");
            }

            // Desactivar usuario
            u.Activo = false;
            await _unitOfWork.Usuarios.UpdateAsync(u, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Usuario desactivado exitosamente.");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password.Trim()));
            return Convert.ToHexString(bytes);
        }

        private static UsuarioDto MapToDto(Usuario u)
        {
            return new UsuarioDto
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                NombreUsuario = u.NombreUsuario,
                Cedula = u.Cedula,
                Telefono = u.Telefono,
                Rol = u.Rol,
                PorcentajeComision = u.PorcentajeComision,
                UltimoAcceso = u.UltimoAcceso,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            };
        }
    }
}
