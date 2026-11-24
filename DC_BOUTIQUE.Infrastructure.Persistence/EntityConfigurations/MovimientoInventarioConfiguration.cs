using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
    {
        public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
        {
            builder.ToTable("MovimientosInventario");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.DocumentoReferencia).HasMaxLength(80);
            builder.Property(m => m.Motivo).HasMaxLength(300);
            builder.Property(m => m.CostoUnitarioReferencia).HasPrecision(18, 2);

            builder.HasOne(m => m.ProductoVariante)
                   .WithMany(v => v.MovimientosInventario)
                   .HasForeignKey(m => m.ProductoVarianteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Usuario)
                   .WithMany(u => u.MovimientosInventario)
                   .HasForeignKey(m => m.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
