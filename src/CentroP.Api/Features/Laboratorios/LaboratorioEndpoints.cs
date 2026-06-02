using MediatR;

namespace CentroP.Api.Features.Laboratorios;

public static class LaboratorioEndpoints
{
    public static IEndpointRouteBuilder MapLaboratoriosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/laboratorios")
            .WithTags("Laboratorios")
            .RequireRateLimiting("fixed");

        group.MapGet("/", GetAll)
            .WithName("GetAllLaboratorios")
            .WithSummary("Lista completa de laboratorios");

        return app;
    }

    static async Task<IResult> GetAll(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllLaboratoriosQuery(), ct);
        return Results.Ok(result);
    }
}
