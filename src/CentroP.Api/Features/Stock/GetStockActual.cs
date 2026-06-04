using CentroP.Api.Common.Interfaces;
using CentroP.Api.Common.Pagination;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Stock;

public sealed record GetStockActualQuery(
    int Page = 1,
    int PageSize = 20,
    int? IdSucursal = null,
    string? Search = null)
    : IRequest<PagedResult<StockActualDto>>;

public sealed record StockActualDto(
    int IdProducto,
    string Codigo,
    string NombreCompleto,
    int IdSucursal,
    int StockActual,
    DateTime? ProximoVencimiento);

public sealed class GetStockActualHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<GetStockActualQuery, PagedResult<StockActualDto>>
{
    public async Task<PagedResult<StockActualDto>> Handle(
        GetStockActualQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(*) FROM (
                SELECT lv.IdProducto, lv.IdSucursal
                FROM stk_LoteVencimiento lv
                INNER JOIN pro_Producto p ON p.Id = lv.IdProducto
                WHERE lv.Cantidad > 0
                  AND (@IdSucursal IS NULL OR lv.IdSucursal = @IdSucursal)
                  AND (@Search IS NULL
                       OR p.NombreCompleto LIKE '%' + @Search + '%'
                       OR p.Codigo LIKE '%' + @Search + '%')
                GROUP BY lv.IdProducto, lv.IdSucursal
            ) AS cnt;

            SELECT
                p.Id         AS IdProducto,
                p.Codigo,
                p.NombreCompleto,
                lv.IdSucursal,
                SUM(lv.Cantidad)       AS StockActual,
                MIN(lv.Vencimiento)    AS ProximoVencimiento
            FROM stk_LoteVencimiento lv
            INNER JOIN pro_Producto p ON p.Id = lv.IdProducto
            WHERE lv.Cantidad > 0
              AND (@IdSucursal IS NULL OR lv.IdSucursal = @IdSucursal)
              AND (@Search IS NULL
                   OR p.NombreCompleto LIKE '%' + @Search + '%'
                   OR p.Codigo LIKE '%' + @Search + '%')
            GROUP BY p.Id, p.Codigo, p.NombreCompleto, lv.IdSucursal
            ORDER BY p.NombreCompleto, lv.IdSucursal
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var param = new
        {
            request.IdSucursal,
            Search = request.Search,
            Offset = offset,
            PageSize = pageSize
        };

        using var multi = await connection.QueryMultipleAsync(sql, param);

        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<StockActualDto>()).ToList();

        return new PagedResult<StockActualDto>(items, page, pageSize, total);
    }
}
