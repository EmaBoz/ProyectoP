using CentroP.Api.Common.Interfaces;
using CentroP.Api.Common.Messaging;
using Dapper;
using MediatR;

namespace CentroP.Api.Features.Integrations.Inventory;

// ── Request payload (campo "data" del RequestEnvelope) ────────────────────────

public sealed record InventoryRequestPayload(
    string OrderNumber,
    InventoryProviderDto Provider,
    IReadOnlyList<InventoryItemRequestDto> Items);

public sealed record InventoryProviderDto(string Name, string Branch, string Cufe);

public sealed record InventoryItemRequestDto(
    string TroquelCode,
    string AlphabetaCode,
    string BarCode,
    int Quantity);

// ── Query ──────────────────────────────────────────────────────────────────────

public sealed record GetInventoryAvailabilityQuery(
    RequestEnvelope<InventoryRequestPayload> Envelope)
    : IRequest<ResponseEnvelope<InventoryAvailabilityResultDto>>;

// ── Response payload (campo "data" del ResponseEnvelope) ──────────────────────

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
    : IRequestHandler<GetInventoryAvailabilityQuery, ResponseEnvelope<InventoryAvailabilityResultDto>>
{
    public async Task<ResponseEnvelope<InventoryAvailabilityResultDto>> Handle(
        GetInventoryAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var payload = request.Envelope.Data;

        if (!int.TryParse(payload.Provider.Branch, out var idSucursal))
            throw new ArgumentException(
                $"El campo branch '{payload.Provider.Branch}' no es un IdSucursal válido.");

        var barCodes = payload.Items.Select(i => i.BarCode).Distinct().ToArray();

        using var connection = await dbFactory.CreateAsync(cancellationToken);

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

        var resultItems = payload.Items
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

        var data = new InventoryAvailabilityResultDto(
            payload.OrderNumber,
            payload.Provider,
            resultItems);

        return new ResponseEnvelope<InventoryAvailabilityResultDto>(
            Metadata: new EventMetadata(
                EventId: Guid.NewGuid().ToString(),
                TraceId: request.Envelope.Metadata.TraceId),
            Reply: new EventReply(InReplyTo: request.Envelope.Metadata.EventId),
            Data: data);
    }

    private sealed record StockRow(string CodigoBarra, int StockActual);
}
