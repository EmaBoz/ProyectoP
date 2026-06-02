using MediatR;

namespace CentroP.Api.Features.Clientes;

public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/clientes")
            .WithTags("Clientes")
            .RequireRateLimiting("fixed");

        group.MapGet("/", GetAll)
            .WithName("GetAllClientes")
            .WithSummary("Lista paginada de clientes con búsqueda opcional");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetClienteById")
            .WithSummary("Detalle completo de un cliente");

        return app;
    }

    static async Task<IResult> GetAll(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAllClientesQuery(page, pageSize, search), ct);
        return Results.Ok(result);
    }

    static async Task<IResult> GetById(int id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetClienteByIdQuery(id), ct);
        return Results.Ok(result);
    }
}
