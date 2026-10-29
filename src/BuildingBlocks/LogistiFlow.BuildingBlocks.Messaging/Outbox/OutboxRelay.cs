using System.Text.Json;
using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Diagnostics;
using LogistiFlow.BuildingBlocks.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiFlow.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Moves committed outbox rows to the broker. Delivery is at-least-once: a crash between the broker confirm
/// and <c>MarkPublished</c> republishes the row, which consumers absorb through their inbox.
/// </summary>
public sealed partial class OutboxRelay(
    IDbConnectionFactory connectionFactory,
    IMessagePublisher publisher,
    IOptions<OutboxOptions> options,
    ILogger<OutboxRelay> logger)
{
    /// <summary>Publishes one batch and returns how many rows were published.</summary>
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var claimed = (await connection.QueryAsync<OutboxMessage>(new CommandDefinition(
                OutboxQueries.Claim,
                new { LockSeconds = settings.LockDuration.TotalSeconds, settings.BatchSize },
                cancellationToken: cancellationToken)))
            .OrderBy(message => message.Id)
            .ToList();

        var published = 0;
        foreach (var message in claimed)
        {
            try
            {
                using var payload = JsonDocument.Parse(message.Payload);
                var envelope = new MessageEnvelope(
                    message.EventId,
                    message.EventType,
                    message.EventVersion,
                    message.OccurredAt,
                    message.CorrelationId,
                    message.CausationId,
                    payload.RootElement.Clone());

                await publisher.PublishAsync(envelope, message.TraceParent, cancellationToken);
                await connection.ExecuteAsync(new CommandDefinition(OutboxQueries.MarkPublished, new { message.Id }, cancellationToken: cancellationToken));
                MessagingTelemetry.Published.Add(1, new KeyValuePair<string, object?>("event_type", message.EventType));
                published++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var retryDelay = BackoffFor(message.Attempts, settings.MaxRetryDelay);
                LogPublishFailed(logger, exception, message.EventId, message.EventType, message.Attempts, retryDelay.TotalSeconds);
                MessagingTelemetry.PublishFailed.Add(1, new KeyValuePair<string, object?>("event_type", message.EventType));
                await connection.ExecuteAsync(new CommandDefinition(
                    OutboxQueries.MarkFailed,
                    new { message.Id, Error = Truncate(exception.Message, 2000), RetrySeconds = retryDelay.TotalSeconds },
                    cancellationToken: cancellationToken));
            }
        }

        return published;
    }

    public async Task<long> CountPendingAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(OutboxQueries.CountPending, cancellationToken: cancellationToken));
    }

    /// <summary>Exponential backoff (1s, 2s, 4s, ...) capped at <paramref name="max"/>.</summary>
    public static TimeSpan BackoffFor(int attempts, TimeSpan max)
    {
        var exponent = Math.Clamp(attempts - 1, 0, 16);
        var delay = TimeSpan.FromSeconds(Math.Pow(2, exponent));
        return delay < max ? delay : max;
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing outbox event {EventId} ({EventType}) failed on attempt {Attempt}; retrying in {RetrySeconds}s")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception, Guid eventId, string eventType, int attempt, double retrySeconds);
}
