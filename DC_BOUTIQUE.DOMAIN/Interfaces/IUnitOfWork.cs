namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        ICategoriaRepository Categorias { get; }
        IProductoRepository Productos { get; }
        IProductoVarianteRepository ProductoVariantes { get; }
        IClienteRepository Clientes { get; }
        IUsuarioRepository Usuarios { get; }
        ITurnoCajaRepository TurnosCaja { get; }
        IGastoCajaRepository GastosCaja { get; }
        IVentaRepository Ventas { get; }
        IMovimientoInventarioRepository MovimientosInventario { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
