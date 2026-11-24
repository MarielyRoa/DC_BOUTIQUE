using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class TurnoCajaConfiguration : IEntityTypeConfiguration<TurnoCaja>
    {
        public void Configure(EntityTypeBuilder<TurnoCaja> builder)
        {
            builder.ToTable("TurnosCaja");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.CodigoTurno).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Observaciones).HasMaxLength(500);

            builder.Property(t => t.FondoInicialEfectivo).HasPrecision(18, 2);
            builder.Property(t => t.EfectivoContadoAlCierre).HasPrecision(18, 2);
            builder.Property(t => t.DiferenciaArqueo).HasPrecision(18, 2);

            builder.HasOne(t => t.Usuario)
                   .WithMany(u => u.TurnosAperturados)
                   .HasForeignKey(t => t.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
