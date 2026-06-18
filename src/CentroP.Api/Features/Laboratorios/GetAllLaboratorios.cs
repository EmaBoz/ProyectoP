using CentroP.Api.Common.Interfaces;
using CentroP.Api.Infrastructure.Cache;
using Dapper;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace CentroP.Api.Features.Laboratorios;

public sealed record GetAllLaboratoriosQuery : IRequest<IReadOnlyList<LaboratorioDto>>;

public sealed record LaboratorioDto(int Id, string Codigo, string Nombre, int? IdEmpresa);

public sealed class GetAllLaboratoriosHandler(IDbConnectionFactory dbFactory, HybridCache cache)
    : IRequestHandler<GetAllLaboratoriosQuery, IReadOnlyList<LaboratorioDto>>
{
    public async Task<IReadOnlyList<LaboratorioDto>> Handle(
        GetAllLaboratoriosQuery request, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.LaboratoriosAll,
            async ct =>
            {
                using var connection = await dbFactory.CreateAsync(ct);
                const string sql = """
                    SELECT Id, Codigo, Nombre, IdEmpresa
                    FROM cor_Laboratorio
                    ORDER BY Nombre;
                    """;
                return (await connection.QueryAsync<LaboratorioDto>(sql)).ToList();
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) },
            cancellationToken: cancellationToken);
    }
}
