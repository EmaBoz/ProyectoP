using CentroP.Api.Common.Interfaces;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Integrations.Inventory;

// ── DTOs compartidos (Query + Worker) ─────────────────────────────────────────

public sealed record InventoryProviderDto(string Name, string Branch, string Cufe);

public sealed record InventoryItemRequestDto(
    string TroquelCode,
    string AlphabetaCode,
    string BarCode,
    int Quantity);

// ── Query ──────────────────────────────────────────────────────────────────────

public sealed record GetInventoryAvailabilityQuery(
    string OrderNumber,
    InventoryProviderDto Provider,
    IReadOnlyList<InventoryItemRequestDto> Items)
    : IRequest<InventoryAvailabilityResultDto>;

// ── DTOs de salida ─────────────────────────────────────────────────────────────

public sealed record InventoryItemResultDto(
    string TroquelCode,
    string AlphabetaCode,
    string BarCode,
    int Quantity,
    bool Availability);

public sealed record InventoryAvailabilityResultDto(
    string OrderNumber,
    InventoryProviderDto Provider,
    IReadOnlyList<InventoryItemResultDto> Items);

// ── Handler ────────────────────────────────────────────────────────────────────

public sealed class GetInventoryAvailabilityHandler(IDbConnectionFactory dbFactory)
    : IRequestHandler<GetInventoryAvailabilityQuery, InventoryAvailabilityResultDto>
{
    public async Task<InventoryAvailabilityResultDto> Handle(
        GetInventoryAvailabilityQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Provider.Branch, out var idSucursal))
            throw new ArgumentException(
                $"El campo branch '{request.Provider.Branch}' no es un IdSucursal válido.");

        var barCodes = request.Items.Select(i => i.BarCode).Distinct().ToArray();

        using var connection = await dbFactory.CreateAsync(cancellationToken);

        // Misma lógica de JOIN que Scanner, adaptada para IN @BarCodes (evita N+1)
        const string sql = """
            SELECT
                cb.CodigoBarra,
                COALESCE(s.StockActual, 0) AS StockActual
            FROM pro_CodigosBarra cb
            INNER JOIN pro_Producto p ON p.Id = cb.IdProducto
            LEFT  JOIN stk_Stock s
                ON  s.IdProducto = p.Id
                AND s.IdSucursal  = @IdSucursal
            WHERE cb.CodigoBarra IN @BarCodes;
            """;

        var rows = await connection.QueryAsync<StockRow>(
            sql, new { IdSucursal = idSucursal, BarCodes = barCodes });

        var stockMap = rows
            .GroupBy(r => r.CodigoBarra)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.StockActual));

        var resultItems = request.Items
            .Select(item =>
            {
                var stock = stockMap.GetValueOrDefault(item.BarCode, 0);
                return new InventoryItemResultDto(
                    item.TroquelCode,
                    item.AlphabetaCode,
                    item.BarCode,
                    item.Quantity,
                    Availability: stock >= item.Quantity);
            })
            .ToList();

        return new InventoryAvailabilityResultDto(
            request.OrderNumber,
            request.Provider,
            resultItems);
    }

    private sealed record StockRow(string CodigoBarra, int StockActual);
}
