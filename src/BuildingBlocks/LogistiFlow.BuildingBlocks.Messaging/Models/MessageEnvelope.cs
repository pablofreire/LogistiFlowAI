using System.Text.Json;

namespace LogistiFlow.BuildingBlocks.Messaging.Models;

/// <summary>
/// Wire format of every integration event. The metadata travels with the payload so a message is
/// self-describing when inspected in a dead-letter queue.
/// </summary>
public sealed record MessageEnvelope(
    Guid EventId,
    string EventType,
    int Version,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    Guid? CausationId,
    JsonElement Payload)
{
    public string RoutingKey => RoutingKeys.For(EventType, Version);
}
