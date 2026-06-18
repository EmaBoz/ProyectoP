using CentroP.Api.Common.Interfaces;
using CentroP.Api.Common.Pagination;
using CentroP.Api.Infrastructure.Cache;
using Dapper;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace CentroP.Api.Features.Sucursales;

public sealed record GetAllSucursalesQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<SucursalDto>>;

public sealed record SucursalDto(int Id, string? Nombre, string? Telefono, int? IdEmpresa);

public sealed class GetAllSucursalesHandler(IDbConnectionFactory dbFactory, HybridCache cache)
    : IRequestHandler<GetAllSucursalesQuery, PagedResult<SucursalDto>>
{
    public async Task<PagedResult<SucursalDto>> Handle(
        GetAllSucursalesQuery request, CancellationToken cancellationToken)
    {
        var all = await cache.GetOrCreateAsync(
            CacheKeys.SucursalesAll,
            async ct =>
            {
                using var connection = await dbFactory.CreateAsync(ct);
                const string sql = """
                    SELECT Id, Nombre, Telefono, IdEmpresa
                    FROM cor_Sucursal
                    ORDER BY Nombre;
                    """;
                return (await connection.QueryAsync<SucursalDto>(sql)).ToList();
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(15) },
            cancellationToken: cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<SucursalDto>(items, page, pageSize, all.Count);
    }
}
