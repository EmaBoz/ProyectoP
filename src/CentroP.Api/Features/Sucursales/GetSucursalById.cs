using CentroP.Api.Common.Exceptions;
using CentroP.Api.Infrastructure.Cache;
using CentroP.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CentroP.Api.Features.Sucursales;

public sealed record GetSucursalByIdQuery(int Id) : IRequest<SucursalDto>;

public sealed class GetSucursalByIdHandler(CentroPDbContext db, HybridCache cache)
    : IRequestHandler<GetSucursalByIdQuery, SucursalDto>
{
    public async Task<SucursalDto> Handle(
        GetSucursalByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(
            CacheKeys.SucursalById(request.Id),
            async ct =>
            {
                var entity = await db.Sucursales
                    .Where(s => s.Id == request.Id)
                    .Select(s => new SucursalDto(s.Id, s.Nombre, s.Telefono, s.IdEmpresa))
                    .FirstOrDefaultAsync(ct);
                return entity;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(15) },
            cancellationToken: cancellationToken);

        return result ?? throw new NotFoundException(nameof(SucursalDto), request.Id);
    }
}
