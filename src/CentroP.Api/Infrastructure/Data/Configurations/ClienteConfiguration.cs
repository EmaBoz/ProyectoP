using CentroP.Api.Infrastructure.Data.Entities.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentroP.Api.Infrastructure.Data.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<ClienteEntity>
{
    public void Configure(EntityTypeBuilder<ClienteEntity> builder)
    {
        builder.ToTable("cli_Cliente");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(x => x.IdPersona).HasColumnName("IdPersona").IsRequired();
        builder.Property(x => x.Nacimiento).HasColumnName("Nacimiento");
        builder.Property(x => x.HabilitadoCtaCte).HasColumnName("HabilitadoCtaCte").IsRequired();
        builder.Property(x => x.LimiteCtaCte).HasColumnName("LimiteCtaCte").HasPrecision(19, 4);
        builder.Property(x => x.Habilitado).HasColumnName("Habilitado").IsRequired();
        builder.Property(x => x.IdCondicionIva).HasColumnName("IdCondicionIva").IsRequired();
        builder.Property(x => x.IdConvenio).HasColumnName("IdConvenio");
        builder.Property(x => x.Observacion).HasColumnName("Observacion").HasMaxLength(1000);
    }
}
