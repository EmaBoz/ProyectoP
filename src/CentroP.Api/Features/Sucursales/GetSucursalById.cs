using CentroP.Api.Common.Exceptions;
using CentroP.Api.Common.Interfaces;
using CentroP.Api.Infrastructure.Cache;
using Dapper;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace CentroP.Api.Features.Sucursales;

public sealed record GetSucursalByIdQuery(int Id) : IRequest<SucursalDto>;

public sealed class GetSucursalByIdHandler(IDbConnectionFactory dbFactory, HybridCache cache)
    : IRequestHandler<GetSucursalByIdQuery, SucursalDto>
{
    public async Task<SucursalDto> Handle(
        GetSucursalByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(
            CacheKeys.SucursalById(request.Id),
            async ct =>
            {
                using var connection = await dbFactory.CreateAsync(ct);
                const string sql = """
                    SELECT Id, Nombre, Telefono, IdEmpresa
                    FROM cor_Sucursal
                    WHERE Id = @Id;
                    """;
                return await connection.QueryFirstOrDefaultAsync<SucursalDto>(sql, new { request.Id });
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(15) },
            cancellationToken: cancellationToken);

        return result ?? throw new NotFoundException(nameof(SucursalDto), request.Id);
    }
}
