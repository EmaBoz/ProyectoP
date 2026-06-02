using MediatR;

namespace CentroP.Api.Features.Provincias;

public static class ProvinciaEndpoints
{
    public static IEndpointRouteBuilder MapProvinciasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/provincias")
            .WithTags("Provincias")
            .RequireRateLimiting("fixed");

        group.MapGet("/", GetAll)
            .WithName("GetAllProvincias")
            .WithSummary("Lista completa de provincias");

        return app;
    }

    static async Task<IResult> GetAll(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllProvinciasQuery(), ct);
        return Results.Ok(result);
    }
}
