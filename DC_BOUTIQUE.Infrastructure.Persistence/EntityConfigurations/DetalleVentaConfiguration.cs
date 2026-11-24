using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
    {
        public void Configure(EntityTypeBuilder<DetalleVenta> builder)
        {
            builder.ToTable("DetallesVenta");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.SkuSnapshot).IsRequired().HasMaxLength(60);
            builder.Property(d => d.NombreProductoSnapshot).IsRequired().HasMaxLength(150);
            builder.Property(d => d.TallaSnapshot).IsRequired().HasMaxLength(30);
            builder.Property(d => d.ColorSnapshot).IsRequired().HasMaxLength(50);

            builder.Property(d => d.CostoUnitario).HasPrecision(18, 2);
            builder.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            builder.Property(d => d.PorcentajeDescuento).HasPrecision(5, 2);
            builder.Property(d => d.MontoDescuento).HasPrecision(18, 2);
            builder.Property(d => d.MontoImpuesto).HasPrecision(18, 2);
            builder.Property(d => d.SubtotalLinea).HasPrecision(18, 2);

            builder.HasOne(d => d.Venta)
                   .WithMany(v => v.Detalles)
                   .HasForeignKey(d => d.VentaId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.ProductoVariante)
                   .WithMany(v => v.DetallesVenta)
                   .HasForeignKey(d => d.ProductoVarianteId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
