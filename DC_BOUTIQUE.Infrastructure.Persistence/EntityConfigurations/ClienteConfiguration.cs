using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Nombres).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Apellidos).IsRequired().HasMaxLength(100);
            builder.Property(c => c.CedulaORnc).HasMaxLength(20);
            builder.Property(c => c.Telefono).IsRequired().HasMaxLength(25);
            builder.Property(c => c.Correo).HasMaxLength(120);
            builder.Property(c => c.Direccion).HasMaxLength(250);
            builder.Property(c => c.Notas).HasMaxLength(500);
        }
    }
}
