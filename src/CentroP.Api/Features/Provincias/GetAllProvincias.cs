using CentroP.Api.Common.Interfaces;
using CentroP.Api.Infrastructure.Cache;
using Dapper;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace CentroP.Api.Features.Provincias;

public sealed record GetAllProvinciasQuery : IRequest<IReadOnlyList<ProvinciaDto>>;

public sealed record ProvinciaDto(int Id, int CodProvincia, string? Nombre, int? IdPais);

public sealed class GetAllProvinciasHandler(IDbConnectionFactory dbFactory, HybridCache cache)
    : IRequestHandler<GetAllProvinciasQuery, IReadOnlyList<ProvinciaDto>>
{
    public async Task<IReadOnlyList<ProvinciaDto>> Handle(
        GetAllProvinciasQuery request, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.ProvinciasAll,
            async ct =>
            {
                using var connection = await dbFactory.CreateAsync(ct);
                const string sql = """
                    SELECT Id, CodProvincia, Nombre, IdPais
                    FROM cor_Provincia
                    ORDER BY Nombre;
                    """;
                return (await connection.QueryAsync<ProvinciaDto>(sql)).ToList();
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) },
            cancellationToken: cancellationToken);
    }
}
