using DC_BOUTIQUE.Core.Application.DTOs.Usuarios;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
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

        public async Task<bool> ExisteDesarrolladorAsync(CancellationToken cancellationToken = default)
        {
            var usuarios = await _unitOfWork.Usuarios.GetAllListAsync(cancellationToken);
            return usuarios.Any(u => u.Rol == RolUsuario.Desarrollador && u.Activo);
        }

        public async Task<UsuarioDto?> AutenticarAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(nombreUsuario, cancellationToken);
            if (u == null) return null;

            var inputHashUpper = HashPassword(password);
            var inputHashLower = inputHashUpper.ToLowerInvariant();
            if (!string.Equals(u.PasswordHash, inputHashUpper, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(u.PasswordHash, inputHashLower, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

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

            // Seguridad de servidor: nunca permitir asignar rol Desarrollador desde la gestión normal de la boutique
            if (dto.Rol == RolUsuario.Desarrollador)
            {
                return Result<UsuarioDto>.Failure("Operación denegada: no está permitido asignar el rol Desarrollador desde la gestión de la boutique.");
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
                EsPrincipal = false,
                PorcentajeComision = dto.PorcentajeComision,
                Activo = true
            };

            await _unitOfWork.Usuarios.AddAsync(usuario, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UsuarioDto>.Success(MapToDto(usuario), $"Usuario '{usuario.NombreUsuario}' creado correctamente.");
        }

        /// <summary>
        /// Crea la cuenta técnica inicial con rol Desarrollador únicamente durante la configuración inicial segura
        /// cuando aún no existe ninguna cuenta Desarrollador en la instalación.
        /// </summary>
        public async Task<Result<UsuarioDto>> CrearDesarrolladorInicialAsync(SaveUsuarioDto dto, CancellationToken cancellationToken = default)
        {
            if (await ExisteDesarrolladorAsync(cancellationToken))
            {
                return Result<UsuarioDto>.Failure("La cuenta técnica de Desarrollador ya fue configurada en esta instalación.");
            }

            if (string.IsNullOrWhiteSpace(dto.NombreUsuario) || dto.NombreUsuario.Trim().Length < 4)
            {
                return Result<UsuarioDto>.Failure("El nombre de usuario técnico debe tener al menos 4 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            {
                return Result<UsuarioDto>.Failure("La contraseña técnica debe tener al menos 8 caracteres.");
            }

            var existe = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(dto.NombreUsuario.Trim().ToLower(), cancellationToken);
            if (existe != null)
            {
                return Result<UsuarioDto>.Failure($"El nombre de usuario '{dto.NombreUsuario.Trim()}' ya está en uso.");
            }

            var dev = new Usuario
            {
                Id = dto.Id.HasValue && dto.Id.Value != Guid.Empty ? dto.Id.Value : Guid.NewGuid(),
                NombreCompleto = string.IsNullOrWhiteSpace(dto.NombreCompleto) ? "Soporte Técnico DC_BOUTIQUE" : dto.NombreCompleto.Trim(),
                NombreUsuario = dto.NombreUsuario.Trim().ToLower(),
                PasswordHash = HashPassword(dto.Password),
                Cedula = dto.Cedula?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Rol = RolUsuario.Desarrollador,
                EsPrincipal = true,
                PorcentajeComision = 0m,
                Activo = true
            };

            await _unitOfWork.Usuarios.AddAsync(dev, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UsuarioDto>.Success(MapToDto(dev), "Cuenta técnica de Desarrollador configurada exitosamente.");
        }

        public async Task<Result> UpdateAsync(Guid id, SaveUsuarioDto dto, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken);
            if (u == null)
            {
                return Result.Failure("El usuario no existe o ya fue eliminado.");
            }

            // Seguridad de servidor: proteger la cuenta técnica y bloquear escalamiento a Desarrollador
            if (u.Rol == RolUsuario.Desarrollador)
            {
                return Result.Failure("Operación denegada: la cuenta técnica Desarrollador no puede modificarse desde el panel de la boutique.");
            }

            if (dto.Rol == RolUsuario.Desarrollador)
            {
                return Result.Failure("Operación denegada: no está permitido asignar el rol Desarrollador.");
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
            u.PorcentajeComision = dto.PorcentajeComision;

            // El Administrador Principal puede cambiar su nombre/usuario/contraseña,
            // pero jamás perder su rol Administrador ni quedar desactivado
            if (u.EsPrincipal)
            {
                u.Rol = RolUsuario.Administrador;
                u.Activo = true;
            }
            else
            {
                u.Rol = dto.Rol;
                u.Activo = dto.Activo;
            }

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

            if (!string.Equals(u.PasswordHash, HashPassword(passwordActual), StringComparison.OrdinalIgnoreCase))
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

            if (u.EsPrincipal || u.Rol == RolUsuario.Desarrollador)
            {
                return Result.Failure("No es posible desactivar al administrador principal ni a la cuenta técnica Desarrollador.");
            }

            u.Activo = false;
            await _unitOfWork.Usuarios.UpdateAsync(u, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Usuario desactivado exitosamente.");
        }

        public async Task<Result> ActivarAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken);
            if (u == null)
            {
                return Result.Failure("El usuario no existe o ya fue eliminado.");
            }

            if (u.Rol == RolUsuario.Desarrollador)
            {
                return Result.Failure("La cuenta técnica Desarrollador no se administra desde este módulo.");
            }

            u.Activo = true;
            await _unitOfWork.Usuarios.UpdateAsync(u, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success($"Usuario '{u.NombreCompleto}' activado exitosamente.");
        }

        public async Task<Result> HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var u = await _unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken);
            if (u == null)
            {
                return Result.Failure("El usuario no existe o ya fue eliminado.");
            }

            // Bloqueo inmutable por bandera EsPrincipal y rol Desarrollador (resiste cambio de NombreUsuario)
            if (u.EsPrincipal)
            {
                return Result.Failure("No es posible eliminar al administrador principal del sistema, independientemente de su nombre de usuario.");
            }

            if (u.Rol == RolUsuario.Desarrollador)
            {
                return Result.Failure("No es posible eliminar la cuenta técnica Desarrollador.");
            }

            var tieneVentas = _unitOfWork.Ventas.GetAllQuery().Any(v => v.UsuarioId == id);
            if (tieneVentas)
            {
                return Result.Failure($"No se puede eliminar a '{u.NombreCompleto}' porque tiene ventas o facturas registradas en el historial. Puedes desactivarlo para bloquear su acceso.");
            }

            var tieneTurnos = _unitOfWork.TurnosCaja.GetAllQuery().Any(t => t.UsuarioId == id);
            if (tieneTurnos)
            {
                return Result.Failure($"No se puede eliminar a '{u.NombreCompleto}' porque tiene turnos de caja registrados. Puedes desactivarlo para bloquear su acceso.");
            }

            var tieneMovimientos = _unitOfWork.MovimientosInventario.GetAllQuery().Any(m => m.UsuarioId == id);
            if (tieneMovimientos)
            {
                return Result.Failure($"No se puede eliminar a '{u.NombreCompleto}' porque tiene registros en el Kardex / movimientos de inventario.");
            }

            await _unitOfWork.Usuarios.HardDeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success($"Usuario '{u.NombreCompleto}' eliminado definitivamente de la base de datos.");
        }

        public async Task<Result> ResetPasswordByTechAsync(Guid usuarioId, string nuevoPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nuevoPassword) || nuevoPassword.Length < 4)
            {
                return Result.Failure("La nueva contraseña debe tener al menos 4 caracteres.");
            }

            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId, cancellationToken);
            if (usuario == null)
            {
                return Result.Failure("Usuario no encontrado.");
            }

            if (usuario.Rol == RolUsuario.Desarrollador && nuevoPassword.Length < 8)
            {
                return Result.Failure("La contraseña de una cuenta Desarrollador debe tener al menos 8 caracteres.");
            }

            usuario.PasswordHash = HashPassword(nuevoPassword);
            await _unitOfWork.Usuarios.UpdateAsync(usuario, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Contraseña restablecida correctamente.");
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
                EsPrincipal = u.EsPrincipal,
                PorcentajeComision = u.PorcentajeComision,
                UltimoAcceso = u.UltimoAcceso,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            };
        }
    }
}
