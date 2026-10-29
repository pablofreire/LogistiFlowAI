namespace LogistiFlow.BuildingBlocks.Messaging.Models;

/// <summary>A claimed row of <c>messaging.outbox_messages</c>.</summary>
public sealed record OutboxMessage(
    long Id,
    Guid EventId,
    string EventType,
    int EventVersion,
    string Payload,
    string CorrelationId,
    Guid? CausationId,
    string? TraceParent,
    DateTimeOffset OccurredAt,
    int Attempts);
