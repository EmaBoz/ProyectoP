namespace CentroP.Api.Infrastructure.Data.Entities.Core;

public sealed class LaboratorioEntity
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int? IdEmpresa { get; set; }
}
