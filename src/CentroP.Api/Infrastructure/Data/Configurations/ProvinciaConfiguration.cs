using CentroP.Api.Infrastructure.Data.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentroP.Api.Infrastructure.Data.Configurations;

public sealed class ProvinciaConfiguration : IEntityTypeConfiguration<ProvinciaEntity>
{
    public void Configure(EntityTypeBuilder<ProvinciaEntity> builder)
    {
        builder.ToTable("cor_Provincia");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id");
        builder.Property(x => x.CodProvincia).HasColumnName("CodProvincia");
        builder.Property(x => x.Nombre).HasColumnName("Nombre").HasMaxLength(150);
        builder.Property(x => x.IdPais).HasColumnName("IdPais");
    }
}
