using DC_BOUTIQUE.Core.Application.DTOs.Clientes;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;

namespace DC_BOUTIQUE.Core.Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClienteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ClienteDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Limpieza y purga automática de clientes inactivos residuales para asegurar borrado real en base de datos
            var inactivos = _unitOfWork.Clientes.GetAllQuery()
                .Where(c => !c.Activo)
                .ToList();

            if (inactivos.Any())
            {
                foreach (var inactivo in inactivos)
                {
                    var ventas = _unitOfWork.Ventas.GetAllQuery()
                        .Where(v => v.ClienteId == inactivo.Id)
                        .ToList();

                    foreach (var v in ventas)
                    {
                        v.ClienteId = null;
                        await _unitOfWork.Ventas.UpdateAsync(v, cancellationToken);
                    }

                    await _unitOfWork.Clientes.HardDeleteAsync(inactivo.Id, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var clientes = _unitOfWork.Clientes.GetAllQuery()
                .Where(c => c.Activo)
                .OrderByDescending(c => c.FechaCreacion)
                .ToList();

            return clientes.Select(MapToDto).ToList();
        }

        public async Task<ClienteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var c = await _unitOfWork.Clientes.GetByIdAsync(id, cancellationToken);
            return c == null ? null : MapToDto(c);
        }

        public async Task<List<ClienteDto>> BuscarAsync(string filtro, CancellationToken cancellationToken = default)
        {
            var clientes = await _unitOfWork.Clientes.BuscarAsync(filtro, cancellationToken);
            return clientes.Select(MapToDto).ToList();
        }

        public async Task<Result<ClienteDto>> CreateAsync(SaveClienteDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombres))
            {
                return Result<ClienteDto>.Failure("El nombre del cliente es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(dto.Telefono))
            {
                return Result<ClienteDto>.Failure("El teléfono del cliente es obligatorio.");
            }

            var cliente = new Cliente
            {
                Nombres = dto.Nombres.Trim(),
                Apellidos = dto.Apellidos?.Trim() ?? string.Empty,
                CedulaORnc = dto.CedulaORnc?.Trim(),
                Telefono = dto.Telefono.Trim(),
                Correo = dto.Correo?.Trim(),
                Direccion = dto.Direccion?.Trim(),
                Notas = dto.Notas?.Trim(),
                Activo = true
            };

            await _unitOfWork.Clientes.AddAsync(cliente, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ClienteDto>.Success(MapToDto(cliente), "Cliente registrado exitosamente.");
        }

        public async Task<Result> UpdateAsync(Guid id, SaveClienteDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombres))
            {
                return Result.Failure("El nombre del cliente es obligatorio.");
            }

            var c = await _unitOfWork.Clientes.GetByIdAsync(id, cancellationToken);
            if (c == null)
            {
                return Result.Failure("El cliente que intenta modificar no existe o fue eliminado.");
            }

            c.Nombres = dto.Nombres.Trim();
            c.Apellidos = dto.Apellidos?.Trim() ?? string.Empty;
            c.CedulaORnc = dto.CedulaORnc?.Trim();
            c.Telefono = dto.Telefono.Trim();
            c.Correo = dto.Correo?.Trim();
            c.Direccion = dto.Direccion?.Trim();
            c.Notas = dto.Notas?.Trim();

            await _unitOfWork.Clientes.UpdateAsync(c, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success("Datos del cliente actualizados exitosamente.");
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var c = await _unitOfWork.Clientes.GetByIdAsync(id, cancellationToken);
            if (c == null)
            {
                return Result.Failure("El cliente no existe o ya fue eliminado.");
            }

            var nombreCompleto = $"{c.Nombres} {c.Apellidos}".Trim();

            // Desvincular ventas asociadas para preservar la integridad de comprobantes históricos
            var ventas = _unitOfWork.Ventas.GetAllQuery()
                .Where(v => v.ClienteId == id)
                .ToList();

            foreach (var v in ventas)
            {
                v.ClienteId = null;
                await _unitOfWork.Ventas.UpdateAsync(v, cancellationToken);
            }

            // Eliminación física directa y definitiva de la base de datos
            await _unitOfWork.Clientes.HardDeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success($"Cliente '{nombreCompleto}' eliminado permanentemente de la base de datos.");
        }

        private static ClienteDto MapToDto(Cliente c)
        {
            return new ClienteDto
            {
                Id = c.Id,
                Nombres = c.Nombres,
                Apellidos = c.Apellidos,
                CedulaORnc = c.CedulaORnc,
                Telefono = c.Telefono,
                Correo = c.Correo,
                Direccion = c.Direccion,
                Notas = c.Notas,
                Activo = c.Activo,
                FechaCreacion = c.FechaCreacion
            };
        }
    }
}
