using CentroP.Api.Infrastructure.Cache;
using CentroP.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CentroP.Api.Features.Provincias;

public sealed record GetAllProvinciasQuery : IRequest<IReadOnlyList<ProvinciaDto>>;

public sealed record ProvinciaDto(int Id, int CodProvincia, string? Nombre, int? IdPais);

public sealed class GetAllProvinciasHandler(CentroPDbContext db, HybridCache cache)
    : IRequestHandler<GetAllProvinciasQuery, IReadOnlyList<ProvinciaDto>>
{
    public async Task<IReadOnlyList<ProvinciaDto>> Handle(
        GetAllProvinciasQuery request, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.ProvinciasAll,
            async ct => await db.Provincias
                .OrderBy(p => p.Nombre)
                .Select(p => new ProvinciaDto(p.Id, p.CodProvincia, p.Nombre, p.IdPais))
                .ToListAsync(ct),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) },
            cancellationToken: cancellationToken);
    }
}
