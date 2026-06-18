using CentroP.Api.Common.Messaging;
using MediatR;

namespace CentroP.Api.Features.Integrations.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithTags("Orders")
            .RequireRateLimiting("fixed");

        group.MapPost("/quote", ConfirmarCotizacion)
            .WithName("ConfirmarCotizacion")
            .WithSummary("Confirma cotización de medicamentos con precios reales y cobertura ADESFA simulada");

        return app;
    }

    static async Task<IResult> ConfirmarCotizacion(
        RequestEnvelope<ConfirmQuoteRequestPayload> body, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ConfirmQuoteQuery(body), ct);
        return Results.Ok(result);
    }
}
