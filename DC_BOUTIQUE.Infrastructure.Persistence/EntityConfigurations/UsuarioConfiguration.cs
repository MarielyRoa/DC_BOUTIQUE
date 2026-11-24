using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
            builder.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(50);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.Cedula).HasMaxLength(20);
            builder.Property(u => u.Telefono).HasMaxLength(25);
            builder.Property(u => u.PorcentajeComision).HasPrecision(5, 2);

            builder.HasIndex(u => u.NombreUsuario).IsUnique();
        }
    }
}
