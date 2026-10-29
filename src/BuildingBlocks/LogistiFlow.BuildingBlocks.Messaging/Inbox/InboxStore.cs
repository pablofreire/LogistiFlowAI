using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;

namespace LogistiFlow.BuildingBlocks.Messaging.Inbox;

/// <summary>Records processed events per consumer in the current transaction.</summary>
public sealed class InboxStore(IDbSession session)
{
    private const string TryInsert = """
        INSERT INTO messaging.inbox_messages (consumer, event_id, event_type)
        VALUES (@Consumer, @EventId, @EventType)
        ON CONFLICT (consumer, event_id) DO NOTHING
        """;

    /// <summary>Returns <c>false</c> when the event was already processed by <paramref name="consumer"/>.</summary>
    public async Task<bool> TryMarkProcessedAsync(string consumer, Guid eventId, string eventType, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var inserted = await connection.ExecuteAsync(new CommandDefinition(
            TryInsert,
            new { Consumer = consumer, EventId = eventId, EventType = eventType },
            session.Transaction,
            cancellationToken: cancellationToken));
        return inserted == 1;
    }
}
