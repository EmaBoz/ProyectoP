using CentroP.Api.Common.Interfaces;
using CentroP.Api.Common.Pagination;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Stock;

public sealed record GetLotesVencimientoQuery(
    int Page = 1,
    int PageSize = 20,
    int? IdSucursal = null,
    int? IdProducto = null,
    DateTime? VencimientoHasta = null)
    : IRequest<PagedResult<LoteVencimientoDto>>;

public sealed record LoteVencimientoDto(
    int Id,
    int IdSucursal,
    int IdProducto,
    string CodigoProducto,
    string NombreProducto,
    string CodigoLote,
    DateTime? Vencimiento,
    int Cantidad);

public sealed class GetLotesVencimientoHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<GetLotesVencimientoQuery, PagedResult<LoteVencimientoDto>>
{
    public async Task<PagedResult<LoteVencimientoDto>> Handle(
        GetLotesVencimientoQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1)
            FROM stk_LoteVencimiento lv
            INNER JOIN pro_Producto p ON p.Id = lv.IdProducto
            WHERE lv.Cantidad > 0
              AND (@IdSucursal      IS NULL OR lv.IdSucursal  = @IdSucursal)
              AND (@IdProducto      IS NULL OR lv.IdProducto  = @IdProducto)
              AND (@VencimientoHasta IS NULL OR lv.Vencimiento <= @VencimientoHasta);

            SELECT
                lv.Id,
                lv.IdSucursal,
                lv.IdProducto,
                p.Codigo         AS CodigoProducto,
                p.NombreCompleto AS NombreProducto,
                lv.CodigoLote,
                lv.Vencimiento,
                lv.Cantidad
            FROM stk_LoteVencimiento lv
            INNER JOIN pro_Producto p ON p.Id = lv.IdProducto
            WHERE lv.Cantidad > 0
              AND (@IdSucursal      IS NULL OR lv.IdSucursal  = @IdSucursal)
              AND (@IdProducto      IS NULL OR lv.IdProducto  = @IdProducto)
              AND (@VencimientoHasta IS NULL OR lv.Vencimiento <= @VencimientoHasta)
            ORDER BY lv.Vencimiento, p.NombreCompleto, lv.IdSucursal
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var param = new
        {
            request.IdSucursal,
            request.IdProducto,
            request.VencimientoHasta,
            Offset = offset,
            PageSize = pageSize
        };

        using var multi = await connection.QueryMultipleAsync(sql, param);

        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<LoteVencimientoDto>()).ToList();

        return new PagedResult<LoteVencimientoDto>(items, page, pageSize, total);
    }
}
