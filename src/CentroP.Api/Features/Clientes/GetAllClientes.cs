using CentroP.Api.Common.Interfaces;
using CentroP.Api.Common.Pagination;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Clientes;

public sealed record GetAllClientesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null)
    : IRequest<PagedResult<ClienteListDto>>;

public sealed record ClienteListDto(
    int Id,
    string Nombre,
    string? Apellido,
    string? Telefono,
    string? Mail,
    bool Habilitado);

public sealed class GetAllClientesHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<GetAllClientesQuery, PagedResult<ClienteListDto>>
{
    public async Task<PagedResult<ClienteListDto>> Handle(
        GetAllClientesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT
                COUNT(1)
            FROM cli_Cliente c
            INNER JOIN cor_Persona p ON p.Id = c.IdPersona
            WHERE (@Search IS NULL
                OR p.Nombre LIKE '%' + @Search + '%'
                OR p.Apellido LIKE '%' + @Search + '%');

            SELECT
                c.Id,
                p.Nombre,
                p.Apellido,
                p.TelefonoP AS Telefono,
                p.Mail,
                c.Habilitado
            FROM cli_Cliente c
            INNER JOIN cor_Persona p ON p.Id = c.IdPersona
            WHERE (@Search IS NULL
                OR p.Nombre LIKE '%' + @Search + '%'
                OR p.Apellido LIKE '%' + @Search + '%')
            ORDER BY p.Apellido, p.Nombre
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var param = new { Search = request.Search, Offset = offset, PageSize = pageSize };
        using var multi = await connection.QueryMultipleAsync(sql, param);

        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<ClienteListDto>()).ToList();

        return new PagedResult<ClienteListDto>(items, page, pageSize, total);
    }
}
