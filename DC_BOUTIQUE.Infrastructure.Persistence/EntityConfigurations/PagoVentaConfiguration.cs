using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class PagoVentaConfiguration : IEntityTypeConfiguration<PagoVenta>
    {
        public void Configure(EntityTypeBuilder<PagoVenta> builder)
        {
            builder.ToTable("PagosVenta");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Monto).HasPrecision(18, 2);
            builder.Property(p => p.ReferenciaTransaccion).HasMaxLength(100);

            builder.HasOne(p => p.Venta)
                   .WithMany(v => v.Pagos)
                   .HasForeignKey(p => p.VentaId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
