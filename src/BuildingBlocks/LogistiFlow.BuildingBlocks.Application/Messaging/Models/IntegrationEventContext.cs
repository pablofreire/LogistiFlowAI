namespace LogistiFlow.BuildingBlocks.Application.Messaging.Models;

/// <summary>Metadata of the message being handled.</summary>
public sealed record IntegrationEventContext(
    Guid EventId,
    string EventType,
    int Version,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    int Attempt);
