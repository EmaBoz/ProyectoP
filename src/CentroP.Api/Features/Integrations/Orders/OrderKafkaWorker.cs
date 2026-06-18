using Confluent.Kafka;
using CentroP.Api.Common.Messaging;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CentroP.Api.Features.Integrations.Orders;

public sealed class OrderKafkaSettings
{
    public string BootstrapServers { get; init; } = default!;
    public string GroupId { get; init; } = default!;
}

public sealed class OrderKafkaWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OrderKafkaSettings> kafkaOptions,
    ILogger<OrderKafkaWorker> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Suscripción con regex: escucha cualquier CUFE
    private const string ConsumeTopicPattern = "^pharmacy\\..+\\.999\\.order\\.v1\\.confirm-quote-request$";
    private const string ResponseTopic = "farmatouch.order.v1.confirm-quote-result";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var settings = kafkaOptions.Value;

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

        consumer.Subscribe(ConsumeTopicPattern);
        logger.LogInformation(
            "OrderKafkaWorker iniciado. Escuchando patrón '{Pattern}'", ConsumeTopicPattern);

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

                var envelope = JsonSerializer.Deserialize<RequestEnvelope<ConfirmQuoteRequestPayload>>(
                    result.Message.Value, JsonOptions);

                if (envelope?.Metadata is null || envelope.Data is null)
                {
                    logger.LogWarning(
                        "Payload inválido o metadata ausente, mensaje descartado. Offset={Offset}",
                        result.Offset.Value);
                    consumer.Commit(result);
                    continue;
                }

                logger.LogInformation(
                    "Cotización recibida. EventId={EventId} TraceId={TraceId} Topic={Topic} Offset={Offset}",
                    envelope.Metadata.EventId,
                    envelope.Metadata.TraceId,
                    result.Topic,
                    result.Offset.Value);

                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var responseEnvelope = await sender.Send(
                    new ConfirmQuoteQuery(envelope), stoppingToken);

                await producer.ProduceAsync(
                    ResponseTopic,
                    new Message<string, string>
                    {
                        Key = result.Message.Key,
                        Value = JsonSerializer.Serialize(responseEnvelope, JsonOptions)
                    },
                    stoppingToken);

                consumer.Commit(result);

                logger.LogInformation(
                    "Cotización publicada en '{Topic}'. EventId={EventId} TraceId={TraceId}",
                    ResponseTopic,
                    responseEnvelope.Metadata.EventId,
                    responseEnvelope.Metadata.TraceId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex,
                    "Error de consumo Kafka. Reason={Reason}", ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error procesando cotización. Offset={Offset}",
                    result?.Offset.Value.ToString() ?? "desconocido");

                if (result is not null)
                    consumer.Commit(result);
            }
        }

        consumer.Close();
        logger.LogInformation("OrderKafkaWorker detenido.");
    }
}
