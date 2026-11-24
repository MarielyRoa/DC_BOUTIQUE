using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DCBoutiqueContext _context;
        private IDbContextTransaction? _transaction;

        public ICategoriaRepository Categorias { get; }
        public IProductoRepository Productos { get; }
        public IProductoVarianteRepository ProductoVariantes { get; }
        public IClienteRepository Clientes { get; }
        public IUsuarioRepository Usuarios { get; }
        public ITurnoCajaRepository TurnosCaja { get; }
        public IGastoCajaRepository GastosCaja { get; }
        public IVentaRepository Ventas { get; }
        public IMovimientoInventarioRepository MovimientosInventario { get; }

        public UnitOfWork(
            DCBoutiqueContext context,
            ICategoriaRepository categorias,
            IProductoRepository productos,
            IProductoVarianteRepository productoVariantes,
            IClienteRepository clientes,
            IUsuarioRepository usuarios,
            ITurnoCajaRepository turnosCaja,
            IGastoCajaRepository gastosCaja,
            IVentaRepository ventas,
            IMovimientoInventarioRepository movimientosInventario)
        {
            _context = context;
            Categorias = categorias;
            Productos = productos;
            ProductoVariantes = productoVariantes;
            Clientes = clientes;
            Usuarios = usuarios;
            TurnosCaja = turnosCaja;
            GastosCaja = gastosCaja;
            Ventas = ventas;
            MovimientosInventario = movimientosInventario;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            await _context.DisposeAsync();
        }
    }
}
