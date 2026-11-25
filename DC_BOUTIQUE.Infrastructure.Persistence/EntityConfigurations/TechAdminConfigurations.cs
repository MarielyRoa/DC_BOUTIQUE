using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DC_BOUTIQUE.Infrastructure.Persistence.EntityConfigurations
{
    public class RegistroLogTecnicoConfiguration : IEntityTypeConfiguration<RegistroLogTecnico>
    {
        public void Configure(EntityTypeBuilder<RegistroLogTecnico> builder)
        {
            builder.ToTable("RegistrosLogTecnico");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Severidad).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Origen).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Mensaje).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.DetalleTecnico).HasMaxLength(4000);
            builder.Property(x => x.UsuarioRelacionado).HasMaxLength(80);
            builder.HasIndex(x => x.FechaEvento);
            builder.HasIndex(x => x.Tipo);
        }
    }

    public class AuditoriaTecnicaConfiguration : IEntityTypeConfiguration<AuditoriaTecnica>
    {
        public void Configure(EntityTypeBuilder<AuditoriaTecnica> builder)
        {
            builder.ToTable("AuditoriasTecnicas");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UsuarioTecnico).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Modulo).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Accion).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Detalle).HasMaxLength(1500);
            builder.HasIndex(x => x.FechaAccion);
        }
    }

    public class ConfiguracionRespaldosConfiguration : IEntityTypeConfiguration<ConfiguracionRespaldos>
    {
        public void Configure(EntityTypeBuilder<ConfiguracionRespaldos> builder)
        {
            builder.ToTable("ConfiguracionesRespaldos");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.HoraRespaldoDiario).HasMaxLength(10).IsRequired();
            builder.Property(x => x.CarpetaDestino).HasMaxLength(400).IsRequired();
            builder.Property(x => x.UltimoArchivoRespaldo).HasMaxLength(260);
            builder.Property(x => x.UltimoErrorRespaldo).HasMaxLength(1000);
        }
    }

    public class ConfiguracionTecnicaConfiguration : IEntityTypeConfiguration<ConfiguracionTecnica>
    {
        public void Configure(EntityTypeBuilder<ConfiguracionTecnica> builder)
        {
            builder.ToTable("ConfiguracionesTecnicas");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ImpresoraConfigurada).HasMaxLength(180).IsRequired();
            builder.Property(x => x.NombreComercialTicket).HasMaxLength(150).IsRequired();
            builder.Property(x => x.DireccionTicket).HasMaxLength(250).IsRequired();
            builder.Property(x => x.TelefonoTicket).HasMaxLength(60).IsRequired();
            builder.Property(x => x.MensajePieTicket).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Moneda).HasMaxLength(10).IsRequired();
            builder.Property(x => x.FormatoFecha).HasMaxLength(30).IsRequired();
            builder.Property(x => x.ZonaHoraria).HasMaxLength(100).IsRequired();
            builder.Property(x => x.CulturaRegional).HasMaxLength(20).IsRequired();
            builder.Property(x => x.VersionRegistrada).HasMaxLength(30).IsRequired();
        }
    }

    public class RegistroMantenimientoConfiguration : IEntityTypeConfiguration<RegistroMantenimiento>
    {
        public void Configure(EntityTypeBuilder<RegistroMantenimiento> builder)
        {
            builder.ToTable("RegistrosMantenimiento");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DescripcionProblema).HasMaxLength(1500).IsRequired();
            builder.Property(x => x.SolucionAplicada).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.VersionSistema).HasMaxLength(30).IsRequired();
            builder.Property(x => x.VersionAnterior).HasMaxLength(30);
            builder.Property(x => x.Resultado).HasMaxLength(60).IsRequired();
            builder.Property(x => x.Observaciones).HasMaxLength(1500);
            builder.Property(x => x.RegistradoPor).HasMaxLength(80).IsRequired();
            builder.HasIndex(x => x.FechaMantenimientoUtc);
            builder.HasIndex(x => x.Tipo);
        }
    }
}
