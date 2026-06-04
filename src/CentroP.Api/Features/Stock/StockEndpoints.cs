using MediatR;

namespace CentroP.Api.Features.Stock;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/stock")
            .WithTags("Stock")
            .RequireRateLimiting("fixed");

        group.MapGet("/actual", GetStockActual)
            .WithName("GetStockActual")
            .WithSummary("Stock actual por producto agregado por sucursal");

        group.MapGet("/movimientos", GetMovimientos)
            .WithName("GetMovimientosStock")
            .WithSummary("Lista paginada de movimientos de stock");

        group.MapGet("/movimientos/{idSucursal:int}/{id:int}", GetMovimientoById)
            .WithName("GetMovimientoStockById")
            .WithSummary("Detalle de un movimiento de stock con sus lotes");

        group.MapGet("/lotes", GetLotes)
            .WithName("GetLotesVencimiento")
            .WithSummary("Stock actual desglosado por lote y vencimiento");

        return app;
    }

    static async Task<IResult> GetStockActual(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        int? idSucursal = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetStockActualQuery(page, pageSize, idSucursal, search), ct);
        return Results.Ok(result);
    }

    static async Task<IResult> GetMovimientos(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        int? idSucursal = null,
        int? idProducto = null,
        int? idTipoMovimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetMovimientosStockQuery(page, pageSize, idSucursal, idProducto, idTipoMovimiento, fechaDesde, fechaHasta), ct);
        return Results.Ok(result);
    }

    static async Task<IResult> GetMovimientoById(
        int idSucursal, int id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetMovimientoStockByIdQuery(idSucursal, id), ct);
        return Results.Ok(result);
    }

    static async Task<IResult> GetLotes(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        int? idSucursal = null,
        int? idProducto = null,
        DateTime? vencimientoHasta = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetLotesVencimientoQuery(page, pageSize, idSucursal, idProducto, vencimientoHasta), ct);
        return Results.Ok(result);
    }
}
