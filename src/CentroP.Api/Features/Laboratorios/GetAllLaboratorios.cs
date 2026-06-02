using CentroP.Api.Infrastructure.Cache;
using CentroP.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CentroP.Api.Features.Laboratorios;

public sealed record GetAllLaboratoriosQuery : IRequest<IReadOnlyList<LaboratorioDto>>;

public sealed record LaboratorioDto(int Id, string Codigo, string Nombre, int? IdEmpresa);

public sealed class GetAllLaboratoriosHandler(CentroPDbContext db, HybridCache cache)
    : IRequestHandler<GetAllLaboratoriosQuery, IReadOnlyList<LaboratorioDto>>
{
    public async Task<IReadOnlyList<LaboratorioDto>> Handle(
        GetAllLaboratoriosQuery request, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.LaboratoriosAll,
            async ct => await db.Laboratorios
                .OrderBy(l => l.Nombre)
                .Select(l => new LaboratorioDto(l.Id, l.Codigo, l.Nombre, l.IdEmpresa))
                .ToListAsync(ct),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) },
            cancellationToken: cancellationToken);
    }
}
