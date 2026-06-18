using CentroP.Api.Common.Messaging;
using MediatR;

namespace CentroP.Api.Features.Integrations.Inventory;

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
        RequestEnvelope<InventoryRequestPayload> body, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInventoryAvailabilityQuery(body), ct);
        return Results.Ok(result);
    }
}
