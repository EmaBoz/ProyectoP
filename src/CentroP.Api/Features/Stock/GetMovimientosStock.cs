using CentroP.Api.Common.Interfaces;
using CentroP.Api.Common.Pagination;
using Dapper;
using FluentValidation;
using MediatR;

namespace CentroP.Api.Features.Stock;

public sealed record GetMovimientosStockQuery(
    int Page = 1,
    int PageSize = 20,
    int? IdSucursal = null,
    int? IdProducto = null,
    int? IdTipoMovimiento = null,
    DateTime? FechaDesde = null,
    DateTime? FechaHasta = null)
    : IRequest<PagedResult<MovimientoStockListDto>>;

public sealed record MovimientoStockListDto(
    int Id,
    int IdSucursal,
    int IdProducto,
    string CodigoProducto,
    string NombreProducto,
    int IdTipoMovimiento,
    string? TipoMovimiento,
    DateTime Fecha,
    string? NroReferencia,
    int Variacion,
    int StockAnterior,
    int StockResultante,
    bool? Conciliado);

public sealed class GetMovimientosStockValidator : AbstractValidator<GetMovimientosStockQuery>
{
    public GetMovimientosStockValidator()
    {
        RuleFor(x => x.FechaDesde)
            .Must((query, fechaDesde) => fechaDesde <= query.FechaHasta)
            .When(x => x.FechaDesde.HasValue && x.FechaHasta.HasValue)
            .WithMessage("FechaDesde debe ser menor o igual a FechaHasta.");
    }
}

public sealed class GetMovimientosStockHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<GetMovimientosStockQuery, PagedResult<MovimientoStockListDto>>
{
    public async Task<PagedResult<MovimientoStockListDto>> Handle(
        GetMovimientosStockQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var connection = await dbFactory.CreateAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1)
            FROM stk_MovimientoStock m
            WHERE (@IdSucursal       IS NULL OR m.IdSucursal       = @IdSucursal)
              AND (@IdProducto       IS NULL OR m.IdProducto       = @IdProducto)
              AND (@IdTipoMovimiento IS NULL OR m.IdTipoMovimiento = @IdTipoMovimiento)
              AND (@FechaDesde       IS NULL OR m.Fecha            >= @FechaDesde)
              AND (@FechaHasta       IS NULL OR m.Fecha            <= @FechaHasta);

            SELECT
                m.Id,
                m.IdSucursal,
                m.IdProducto,
                p.Codigo                        AS CodigoProducto,
                p.NombreCompleto                AS NombreProducto,
                m.IdTipoMovimiento,
                t.Nombre                        AS TipoMovimiento,
                m.Fecha,
                m.NroReferencia,
                m.Variacion,
                m.StockAnterior,
                m.StockAnterior + m.Variacion   AS StockResultante,
                m.Conciliado
            FROM stk_MovimientoStock m
            INNER JOIN pro_Producto p ON p.Id = m.IdProducto
            LEFT  JOIN stk_TipoMovimiento t ON t.Id = m.IdTipoMovimiento
            WHERE (@IdSucursal       IS NULL OR m.IdSucursal       = @IdSucursal)
              AND (@IdProducto       IS NULL OR m.IdProducto       = @IdProducto)
              AND (@IdTipoMovimiento IS NULL OR m.IdTipoMovimiento = @IdTipoMovimiento)
              AND (@FechaDesde       IS NULL OR m.Fecha            >= @FechaDesde)
              AND (@FechaHasta       IS NULL OR m.Fecha            <= @FechaHasta)
            ORDER BY m.Fecha DESC, m.IdSucursal DESC, m.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var param = new
        {
            request.IdSucursal,
            request.IdProducto,
            request.IdTipoMovimiento,
            request.FechaDesde,
            request.FechaHasta,
            Offset = offset,
            PageSize = pageSize
        };

        using var multi = await connection.QueryMultipleAsync(sql, param);

        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<MovimientoStockListDto>()).ToList();

        return new PagedResult<MovimientoStockListDto>(items, page, pageSize, total);
    }
}
