using System.Diagnostics;
using System.Text.Json;
using Dapper;
using LogistiFlow.BuildingBlocks.Application.Correlation.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.BuildingBlocks.Messaging.Outbox;

/// <summary>Writes integration events to the outbox table inside the caller's transaction.</summary>
public sealed class OutboxIntegrationEventWriter(IDbSession session, ICorrelationContext correlation) : IIntegrationEventOutbox
{
    public async Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (session.Transaction is null)
        {
            throw new InvalidOperationException(
                "Integration events must be enqueued inside a unit of work so they commit with the state change.");
        }

        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            OutboxQueries.Insert,
            new
            {
                EventId = Guid.CreateVersion7(),
                EventType = TEvent.EventType,
                EventVersion = TEvent.Version,
                Payload = JsonSerializer.Serialize(integrationEvent, MessagingJson.Options),
                correlation.CorrelationId,
                correlation.CausationId,
                TraceParent = Activity.Current?.Id,
            },
            session.Transaction,
            cancellationToken: cancellationToken));
    }
}
