using System.Diagnostics;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Messaging.Diagnostics;
using LogistiFlow.BuildingBlocks.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Options;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.BuildingBlocks.Messaging.Topology;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LogistiFlow.BuildingBlocks.Messaging.Publishing;

/// <summary>
/// Publishes persistent messages on a channel with publisher confirmations: <see cref="PublishAsync"/> completes
/// only when the broker has taken responsibility for the message, and throws otherwise, so the outbox row stays
/// pending and is retried. A channel is not safe for concurrent publishes, hence the gate.
/// </summary>
public sealed class RabbitMqMessagePublisher(IRabbitMqConnectionProvider connectionProvider, IOptions<RabbitMqOptions> options)
    : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(MessageEnvelope envelope, string? traceParent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        using var activity = MessagingTelemetry.ActivitySource.StartActivity(
            $"publish {envelope.EventType}",
            ActivityKind.Producer,
            ActivityContext.TryParse(traceParent, null, out var parent) ? parent : default);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", options.Value.Exchange);
        activity?.SetTag("messaging.message.id", envelope.EventId.ToString());

        var properties = new BasicProperties
        {
            MessageId = envelope.EventId.ToString(),
            Type = envelope.EventType,
            CorrelationId = envelope.CorrelationId,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(envelope.OccurredAt.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                [MessageHeaders.EventVersion] = envelope.Version,
                [MessageHeaders.Attempt] = 1,
                [MessageHeaders.TraceParent] = activity?.Id ?? traceParent,
            },
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, MessagingJson.Options);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            await channel.BasicPublishAsync(options.Value.Exchange, envelope.RoutingKey, mandatory: false, properties, body, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        _gate.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await RabbitMqTopology.DeclareExchangeAsync(_channel, options.Value.Exchange, cancellationToken);
        return _channel;
    }
}
