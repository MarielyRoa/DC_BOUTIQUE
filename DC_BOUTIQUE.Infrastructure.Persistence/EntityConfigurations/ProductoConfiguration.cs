using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("Productos");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.CodigoModelo).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Descripcion).HasMaxLength(500);
            builder.Property(p => p.Marca).HasMaxLength(100);
            builder.Property(p => p.MaterialPrincipal).HasMaxLength(100);
            builder.Property(p => p.ImagenUrl).HasMaxLength(500);

            builder.Property(p => p.CostoBaseReferencia).HasPrecision(18, 2);
            builder.Property(p => p.PrecioVentaBase).HasPrecision(18, 2);
            builder.Property(p => p.PrecioOferta).HasPrecision(18, 2);
            builder.Property(p => p.PorcentajeImpuesto).HasPrecision(5, 2);

            builder.HasIndex(p => p.CodigoModelo).IsUnique();

            builder.HasOne(p => p.Categoria)
                   .WithMany(c => c.Productos)
                   .HasForeignKey(p => p.CategoriaId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
