using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class GastoCajaConfiguration : IEntityTypeConfiguration<GastoCaja>
    {
        public void Configure(EntityTypeBuilder<GastoCaja> builder)
        {
            builder.ToTable("GastosCaja");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Concepto).IsRequired().HasMaxLength(200);
            builder.Property(g => g.CategoriaGasto).IsRequired().HasMaxLength(80);
            builder.Property(g => g.Monto).HasPrecision(18, 2);
            builder.Property(g => g.ComprobanteReferencia).HasMaxLength(80);
            builder.Property(g => g.Observaciones).HasMaxLength(300);

            builder.HasOne(g => g.TurnoCaja)
                   .WithMany(t => t.Gastos)
                   .HasForeignKey(g => g.TurnoCajaId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
