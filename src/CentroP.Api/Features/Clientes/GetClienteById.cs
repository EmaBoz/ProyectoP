using CentroP.Api.Common.Exceptions;
using CentroP.Api.Common.Interfaces;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Clientes;

public sealed record GetClienteByIdQuery(int Id) : IRequest<ClienteDetailDto>;

public sealed record ClienteDetailDto(
    int Id,
    string Nombre,
    string? Apellido,
    string? TelefonoParticular,
    string? TelefonoTrabajo,
    string? Celular,
    string? Mail,
    string? TipoDocumento,
    long? NumeroDocumento,
    string? Cuil,
    DateTime? Nacimiento,
    bool HabilitadoCtaCte,
    decimal? LimiteCtaCte,
    bool Habilitado,
    int IdCondicionIva,
    string? Observacion);

public sealed class GetClienteByIdHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<GetClienteByIdQuery, ClienteDetailDto>
{
    public async Task<ClienteDetailDto> Handle(
        GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT
                c.Id,
                p.Nombre,
                p.Apellido,
                p.TelefonoP AS TelefonoParticular,
                p.TelefonoT AS TelefonoTrabajo,
                p.Celular,
                p.Mail,
                td.Nombre AS TipoDocumento,
                p.NumeroDocumento,
                p.Cuil,
                c.Nacimiento,
                c.HabilitadoCtaCte,
                c.LimiteCtaCte,
                c.Habilitado,
                c.IdCondicionIva,
                c.Observacion
            FROM cli_Cliente c
            INNER JOIN cor_Persona p ON p.Id = c.IdPersona
            LEFT JOIN cor_TipoDocumento td ON td.Id = p.IdTipoDocumento
            WHERE c.Id = @Id;
            """;

        var result = await connection.QuerySingleOrDefaultAsync<ClienteDetailDto>(sql, new { request.Id });

        return result ?? throw new NotFoundException("Cliente", request.Id);
    }
}
