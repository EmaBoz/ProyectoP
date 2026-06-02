using CentroP.Api.Infrastructure.Data.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentroP.Api.Infrastructure.Data.Configurations;

public sealed class SucursalConfiguration : IEntityTypeConfiguration<SucursalEntity>
{
    public void Configure(EntityTypeBuilder<SucursalEntity> builder)
    {
        builder.ToTable("cor_Sucursal");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id");
        builder.Property(x => x.Nombre).HasColumnName("Nombre").HasMaxLength(20);
        builder.Property(x => x.Telefono).HasColumnName("Telefono").HasMaxLength(50);
        builder.Property(x => x.IdDireccion).HasColumnName("IdDireccion");
        builder.Property(x => x.IdEmpresa).HasColumnName("IdEmpresa");
    }
}
