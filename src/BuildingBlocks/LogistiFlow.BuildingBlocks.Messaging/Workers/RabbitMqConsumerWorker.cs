using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Messaging.Consuming;
using LogistiFlow.BuildingBlocks.Messaging.Diagnostics;
using LogistiFlow.BuildingBlocks.Messaging.Enums;
using LogistiFlow.BuildingBlocks.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Options;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.BuildingBlocks.Messaging.Topology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogistiFlow.BuildingBlocks.Messaging.Workers;

/// <summary>
/// Consumes the service queue with manual acknowledgements. Every delivery is acknowledged exactly once, after
/// one of: handled, duplicate (inbox), moved to a retry tier, or moved to the dead-letter queue. A crash before
/// the ack redelivers the message, which the inbox makes harmless.
/// </summary>
public sealed partial class RabbitMqConsumerWorker(
    IRabbitMqConnectionProvider connectionProvider,
    IntegrationEventDispatcher dispatcher,
    IntegrationEventHandlerRegistry registry,
    ConsumerReadiness readiness,
    IOptions<RabbitMqOptions> rabbitOptions,
    IOptions<MessagingOptions> messagingOptions,
    IOptions<ConsumerOptions> consumerOptions,
    ILogger<RabbitMqConsumerWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilChannelFailsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // The consumer must survive broker outages and reconnect.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogConsumerFailed(logger, exception, ReconnectDelay.TotalSeconds);
            }

            try
            {
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ConsumeUntilChannelFailsAsync(CancellationToken stoppingToken)
    {
        var queue = messagingOptions.Value.QueueName;
        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareConsumerQueuesAsync(
            channel,
            rabbitOptions.Value.Exchange,
            queue,
            registry.Registrations.Select(registration => registration.RoutingKey),
            consumerOptions.Value.EffectiveRetryDelays,
            stoppingToken);
        await channel.BasicQosAsync(0, consumerOptions.Value.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnDeliveryAsync(channel, queue, delivery, stoppingToken);
        await channel.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);
        readiness.MarkTopologyDeclared();
        LogConsuming(logger, queue, registry.Registrations.Count);

        // Connection-level failures are healed by automatic recovery. A channel closed while the connection is
        // still open (a channel-level error) is not recovered, so it is recreated here.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(ReconnectDelay, stoppingToken);
            if (!channel.IsOpen && connection.IsOpen)
            {
                return;
            }
        }
    }

    private async Task OnDeliveryAsync(IChannel channel, string queue, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var attempt = ReadAttempt(delivery.BasicProperties);
        MessageEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<MessageEnvelope>(delivery.Body.Span, MessagingJson.Options);
        }
        catch (JsonException exception)
        {
            await DeadLetterAsync(channel, queue, delivery, attempt, $"Malformed message: {exception.Message}", stoppingToken);
            return;
        }

        if (envelope is null)
        {
            await DeadLetterAsync(channel, queue, delivery, attempt, "Empty message body.", stoppingToken);
            return;
        }

        var traceParent = ReadString(delivery.BasicProperties, MessageHeaders.TraceParent);
        using var activity = MessagingTelemetry.ActivitySource.StartActivity(
            $"process {envelope.EventType}",
            ActivityKind.Consumer,
            ActivityContext.TryParse(traceParent, null, out var parent) ? parent : default);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.message.id", envelope.EventId.ToString());
        activity?.SetTag("messaging.consumer.attempt", attempt);

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["EventId"] = envelope.EventId,
            ["EventType"] = envelope.EventType,
        });

        try
        {
            var outcome = await dispatcher.DispatchAsync(envelope, attempt, stoppingToken);
            switch (outcome)
            {
                case DispatchOutcome.Handled:
                    MessagingTelemetry.Consumed.Add(1, new KeyValuePair<string, object?>("event_type", envelope.EventType));
                    LogHandled(logger, envelope.EventType, envelope.EventId, attempt);
                    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                    break;
                case DispatchOutcome.Duplicate:
                    MessagingTelemetry.Duplicates.Add(1, new KeyValuePair<string, object?>("event_type", envelope.EventType));
                    LogDuplicate(logger, envelope.EventType, envelope.EventId);
                    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                    break;
                default:
                    await DeadLetterAsync(channel, queue, delivery, attempt, $"No handler for {envelope.RoutingKey}.", stoppingToken);
                    break;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            if (attempt < consumerOptions.Value.MaxDeliveryAttempts)
            {
                await RetryAsync(channel, queue, delivery, attempt, exception, stoppingToken);
            }
            else
            {
                await DeadLetterAsync(channel, queue, delivery, attempt, exception.Message, stoppingToken);
            }
        }
    }

    private async Task RetryAsync(IChannel channel, string queue, BasicDeliverEventArgs delivery, int attempt, Exception exception, CancellationToken cancellationToken)
    {
        var delays = consumerOptions.Value.EffectiveRetryDelays;
        var tier = Math.Min(attempt, delays.Count) - 1;
        var properties = CopyProperties(delivery.BasicProperties, attempt + 1, reason: null);
        await channel.BasicPublishAsync(string.Empty, RabbitMqTopology.RetryQueue(queue, tier), mandatory: false, properties, delivery.Body, cancellationToken);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        MessagingTelemetry.Retried.Add(1);
        LogRetry(logger, exception, attempt, delays[tier].TotalSeconds);
    }

    private async Task DeadLetterAsync(IChannel channel, string queue, BasicDeliverEventArgs delivery, int attempt, string reason, CancellationToken cancellationToken)
    {
        var properties = CopyProperties(delivery.BasicProperties, attempt, reason);
        await channel.BasicPublishAsync(string.Empty, RabbitMqTopology.DeadLetterQueue(queue), mandatory: false, properties, delivery.Body, cancellationToken);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        MessagingTelemetry.DeadLettered.Add(1);
        LogDeadLettered(logger, attempt, reason);
    }

    private static BasicProperties CopyProperties(IReadOnlyBasicProperties source, int attempt, string? reason)
    {
        var properties = new BasicProperties(source)
        {
            Headers = source.Headers is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(source.Headers),
        };
        properties.Headers[MessageHeaders.Attempt] = attempt;
        if (reason is not null)
        {
            properties.Headers[MessageHeaders.DeadLetterReason] = reason.Length <= 500 ? reason : reason[..500];
        }

        return properties;
    }

    private static int ReadAttempt(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue(MessageHeaders.Attempt, out var value))
        {
            return 1;
        }

        return value switch
        {
            int number => number,
            long number => (int)number,
            byte number => number,
            short number => number,
            _ => 1,
        };
    }

    private static string? ReadString(IReadOnlyBasicProperties properties, string header)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue(header, out var value))
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => null,
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming {Queue} for {HandlerCount} event types")]
    private static partial void LogConsuming(ILogger logger, string queue, int handlerCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {EventType} {EventId} on attempt {Attempt}")]
    private static partial void LogHandled(ILogger logger, string eventType, Guid eventId, int attempt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped duplicate {EventType} {EventId} (already in inbox)")]
    private static partial void LogDuplicate(ILogger logger, string eventType, Guid eventId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handling failed on attempt {Attempt}; retrying in {DelaySeconds}s")]
    private static partial void LogRetry(ILogger logger, Exception exception, int attempt, double delaySeconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message dead-lettered after attempt {Attempt}: {Reason}")]
    private static partial void LogDeadLettered(ILogger logger, int attempt, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Consumer stopped; reconnecting in {DelaySeconds}s")]
    private static partial void LogConsumerFailed(ILogger logger, Exception exception, double delaySeconds);
}
