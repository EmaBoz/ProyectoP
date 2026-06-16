using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CentroP.Api.Features.Integrations.Inventory;

// ── Configuración (enlazada desde appsettings.json → "Kafka") ─────────────────

public sealed class KafkaSettings
{
    public string BootstrapServers { get; init; } = default!;
    public string GroupId { get; init; } = default!;
    public string Cufe { get; init; } = default!;
}

// ── Worker ─────────────────────────────────────────────────────────────────────

public sealed class InventoryKafkaWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaSettings> kafkaOptions,
    ILogger<InventoryKafkaWorker> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const string ResponseTopic = "farmatouch.inventory.v1.stock-result";
    private const string TraceIdHeaderKey = "traceId";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = kafkaOptions.Value;
        var consumeTopic = $"pharmacy.{settings.Cufe}.999.inventory.v1.stock-request";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = false
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe(consumeTopic);
        logger.LogInformation(
            "InventoryKafkaWorker iniciado. Escuchando en '{Topic}'", consumeTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);

                if (result.Message?.Value is null)
                {
                    consumer.Commit(result);
                    continue;
                }

                var traceId = ExtractHeader(result.Message.Headers, TraceIdHeaderKey);

                logger.LogInformation(
                    "Mensaje recibido. TraceId={TraceId} Partition={Partition} Offset={Offset}",
                    traceId, result.Partition.Value, result.Offset.Value);

                var envelope = JsonSerializer.Deserialize<InventoryRequestEnvelope>(
                    result.Message.Value, JsonOptions);

                if (envelope?.Data is null)
                {
                    logger.LogWarning(
                        "Payload inválido, mensaje descartado. TraceId={TraceId}", traceId);
                    consumer.Commit(result);
                    continue;
                }

                var query = MapToQuery(envelope.Data);

                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var queryResult = await sender.Send(query, stoppingToken);

                await producer.ProduceAsync(
                    ResponseTopic,
                    new Message<string, string>
                    {
                        Key = result.Message.Key,
                        Value = JsonSerializer.Serialize(MapToResponseEnvelope(queryResult), JsonOptions),
                        Headers = BuildResponseHeaders(traceId)
                    },
                    stoppingToken);

                consumer.Commit(result);

                logger.LogInformation(
                    "Respuesta publicada en '{Topic}'. OrderNumber={OrderNumber} TraceId={TraceId}",
                    ResponseTopic, envelope.Data.OrderNumber, traceId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex,
                    "Error de consumo Kafka. Reason={Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error procesando mensaje. Offset={Offset}",
                    result?.Offset.Value.ToString() ?? "desconocido");

                // Commit para evitar bloqueo por poison pill
                if (result is not null)
                    consumer.Commit(result);
            }
        }

        consumer.Close();
        logger.LogInformation("InventoryKafkaWorker detenido.");
    }

    private static string? ExtractHeader(Headers? headers, string key)
    {
        if (headers is null) return null;
        var header = headers.FirstOrDefault(h => h.Key == key);
        return header is null ? null : Encoding.UTF8.GetString(header.GetValueBytes());
    }

    private static Headers BuildResponseHeaders(string? traceId)
    {
        var headers = new Headers();
        if (traceId is not null)
            headers.Add(TraceIdHeaderKey, Encoding.UTF8.GetBytes(traceId));
        return headers;
    }

    private static GetInventoryAvailabilityQuery MapToQuery(InventoryRequestData data) =>
        new(
            data.OrderNumber,
            new InventoryProviderDto(data.Provider.Name, data.Provider.Branch, data.Provider.Cufe),
            data.Items.Select(i => new InventoryItemRequestDto(
                i.TroquelCode, i.AlphabetaCode, i.BarCode, i.Quantity)).ToList());

    private static InventoryResponseEnvelope MapToResponseEnvelope(InventoryAvailabilityResultDto result) =>
        new(new InventoryResponseData(
            result.OrderNumber,
            new InventoryProviderJson(
                result.Provider.Name, result.Provider.Branch, result.Provider.Cufe),
            result.Items.Select(i => new InventoryItemResponseJson(
                i.TroquelCode, i.AlphabetaCode, i.BarCode, i.Quantity, i.Availability)).ToList()));

    // ── Contratos JSON privados (Kafka payload) ────────────────────────────────

    private sealed record InventoryRequestEnvelope(InventoryRequestData? Data);

    private sealed record InventoryRequestData(
        string OrderNumber,
        InventoryProviderJson Provider,
        List<InventoryItemJson> Items);

    private sealed record InventoryProviderJson(string Name, string Branch, string Cufe);

    private sealed record InventoryItemJson(
        string TroquelCode,
        string AlphabetaCode,
        string BarCode,
        int Quantity);

    private sealed record InventoryResponseEnvelope(InventoryResponseData Data);

    private sealed record InventoryResponseData(
        string OrderNumber,
        InventoryProviderJson Provider,
        List<InventoryItemResponseJson> Items);

    private sealed record InventoryItemResponseJson(
        string TroquelCode,
        string AlphabetaCode,
        string BarCode,
        int Quantity,
        bool Availability);
}
