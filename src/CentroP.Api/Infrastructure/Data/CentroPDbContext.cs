using CentroP.Api.Infrastructure.Data.Entities.Clientes;
using CentroP.Api.Infrastructure.Data.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace CentroP.Api.Infrastructure.Data;

public sealed class CentroPDbContext(DbContextOptions<CentroPDbContext> opts) : DbContext(opts)
{
    public DbSet<SucursalEntity> Sucursales => Set<SucursalEntity>();
    public DbSet<ProvinciaEntity> Provincias => Set<ProvinciaEntity>();
    public DbSet<LaboratorioEntity> Laboratorios => Set<LaboratorioEntity>();
    public DbSet<ClienteEntity> Clientes => Set<ClienteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CentroPDbContext).Assembly);
    }
}
