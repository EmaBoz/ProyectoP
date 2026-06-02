using System.Data;

namespace CentroP.Api.Common.Interfaces;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateAsync(CancellationToken ct = default);
}
