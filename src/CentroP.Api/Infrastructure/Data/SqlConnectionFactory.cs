using System.Data;
using CentroP.Api.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace CentroP.Api.Infrastructure.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("CentroP")
        ?? throw new InvalidOperationException("Connection string 'CentroP' no encontrada.");

    public async Task<IDbConnection> CreateAsync(CancellationToken ct = default)
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
