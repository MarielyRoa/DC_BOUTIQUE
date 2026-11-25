using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class ProductoVarianteConfiguration : IEntityTypeConfiguration<ProductoVariante>
    {
        public void Configure(EntityTypeBuilder<ProductoVariante> builder)
        {
            builder.ToTable("ProductoVariantes");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Sku).IsRequired().HasMaxLength(60);
            builder.Property(v => v.CodigoBarras).HasMaxLength(80);
            builder.Property(v => v.Talla).IsRequired().HasMaxLength(30);
            builder.Property(v => v.Color).IsRequired().HasMaxLength(50);

            // Token de concurrencia optimista para SQLite
            builder.Property(v => v.ConcurrencyToken).IsConcurrencyToken();

            builder.HasIndex(v => v.Sku).IsUnique();
            builder.HasIndex(v => v.CodigoBarras);

            builder.HasOne(v => v.Producto)
                   .WithMany(p => p.Variantes)
                   .HasForeignKey(v => v.ProductoId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
