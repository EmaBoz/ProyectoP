namespace CentroP.Api.Infrastructure.Data.Entities.Core;

public sealed class SucursalEntity
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Telefono { get; set; }
    public int? IdDireccion { get; set; }
    public int? IdEmpresa { get; set; }
}
