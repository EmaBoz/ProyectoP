using MediatR;

namespace CentroP.Api.Features.Sucursales;

public static class SucursalEndpoints
{
    public static IEndpointRouteBuilder MapSucursalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sucursales")
            .WithTags("Sucursales")
            .RequireRateLimiting("fixed");

        group.MapGet("/", GetAll)
            .WithName("GetAllSucursales")
            .WithSummary("Lista paginada de sucursales");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetSucursalById")
            .WithSummary("Obtiene una sucursal por ID");

        return app;
    }

    static async Task<IResult> GetAll(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAllSucursalesQuery(page, pageSize), ct);
        return Results.Ok(result);
    }

    static async Task<IResult> GetById(
        int id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetSucursalByIdQuery(id), ct);
        return Results.Ok(result);
    }
}
