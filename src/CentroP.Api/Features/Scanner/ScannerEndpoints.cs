using MediatR;

namespace CentroP.Api.Features.Scanner;

public static class ScannerEndpoints
{
    public static IEndpointRouteBuilder MapScannerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scanner")
            .WithTags("Scanner")
            .RequireRateLimiting("fixed");

        group.MapGet("/{idSucursal:int}/{codigoBarra}", Escanear)
            .WithName("EscanearProducto")
            .WithSummary("Precio y stock físico de un producto por código de barras — consulta directa sin caché");

        return app;
    }

    static async Task<IResult> Escanear(
        int idSucursal, string codigoBarra, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new EscanearProductoQuery(idSucursal, codigoBarra), ct);
        return Results.Ok(result);
    }
}
