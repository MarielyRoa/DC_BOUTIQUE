using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class VentaConfiguration : IEntityTypeConfiguration<Venta>
    {
        public void Configure(EntityTypeBuilder<Venta> builder)
        {
            builder.ToTable("Ventas");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.NumeroFactura).IsRequired().HasMaxLength(50);
            builder.Property(v => v.Ncf).HasMaxLength(30);
            builder.Property(v => v.Observaciones).HasMaxLength(500);

            builder.Property(v => v.Subtotal).HasPrecision(18, 2);
            builder.Property(v => v.DescuentoTotal).HasPrecision(18, 2);
            builder.Property(v => v.ImpuestoTotal).HasPrecision(18, 2);
            builder.Property(v => v.TotalNeto).HasPrecision(18, 2);

            builder.HasIndex(v => v.NumeroFactura).IsUnique();

            builder.HasOne(v => v.Cliente)
                   .WithMany(c => c.ComprasRealizadas)
                   .HasForeignKey(v => v.ClienteId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(v => v.Usuario)
                   .WithMany(u => u.VentasRealizadas)
                   .HasForeignKey(v => v.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.TurnoCaja)
                   .WithMany(t => t.Ventas)
                   .HasForeignKey(v => v.TurnoCajaId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
