using CentroP.Api.Infrastructure.Data.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentroP.Api.Infrastructure.Data.Configurations;

public sealed class LaboratorioConfiguration : IEntityTypeConfiguration<LaboratorioEntity>
{
    public void Configure(EntityTypeBuilder<LaboratorioEntity> builder)
    {
        builder.ToTable("cor_Laboratorio");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id");
        builder.Property(x => x.Codigo).HasColumnName("Codigo").HasMaxLength(10).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("Nombre").HasMaxLength(200).IsRequired();
        builder.Property(x => x.IdEmpresa).HasColumnName("IdEmpresa");
    }
}
