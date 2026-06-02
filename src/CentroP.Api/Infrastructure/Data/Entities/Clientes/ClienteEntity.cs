namespace CentroP.Api.Infrastructure.Data.Entities.Clientes;

public sealed class ClienteEntity
{
    public int Id { get; set; }
    public int IdPersona { get; set; }
    public DateTime? Nacimiento { get; set; }
    public bool HabilitadoCtaCte { get; set; }
    public decimal? LimiteCtaCte { get; set; }
    public bool Habilitado { get; set; }
    public int IdCondicionIva { get; set; }
    public int? IdConvenio { get; set; }
    public string? Observacion { get; set; }
}
