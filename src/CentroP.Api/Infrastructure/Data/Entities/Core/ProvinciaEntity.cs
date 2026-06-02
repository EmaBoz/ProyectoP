namespace CentroP.Api.Infrastructure.Data.Entities.Core;

public sealed class ProvinciaEntity
{
    public int Id { get; set; }
    public int CodProvincia { get; set; }
    public string? Nombre { get; set; }
    public int? IdPais { get; set; }
}
