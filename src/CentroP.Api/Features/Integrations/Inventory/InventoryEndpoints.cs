using MediatR;

namespace CentroP.Api.Features.Integrations.Inventory;

// ── HTTP envelopes (misma forma que el contrato Kafka) ─────────────────────────

public sealed record InventoryStockHttpRequest(InventoryStockRequestData Data);

public sealed record InventoryStockRequestData(
    string OrderNumber,
    InventoryProviderDto Provider,
    IReadOnlyList<InventoryItemRequestDto> Items);

public sealed record InventoryStockHttpResponse(InventoryAvailabilityResultDto Data);

// ── Endpoint ───────────────────────────────────────────────────────────────────

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inventory")
            .WithTags("Inventory")
            .RequireRateLimiting("fixed");

        group.MapPost("/stock", ConsultarStock)
            .WithName("ConsultarStockInventario")
            .WithSummary("Consulta disponibilidad de stock para una lista de barcodes en una sucursal");

        return app;
    }

    static async Task<IResult> ConsultarStock(
        InventoryStockHttpRequest body, IMediator mediator, CancellationToken ct)
    {
        var query = new GetInventoryAvailabilityQuery(
            body.Data.OrderNumber,
            body.Data.Provider,
            body.Data.Items);

        var result = await mediator.Send(query, ct);
        return Results.Ok(new InventoryStockHttpResponse(result));
    }
}
