using LogistiFlow.BuildingBlocks.Messaging.Models;

namespace LogistiFlow.BuildingBlocks.Messaging.Interfaces;

/// <summary>Publishes an envelope to the broker and completes only after the broker confirms it.</summary>
public interface IMessagePublisher
{
    Task PublishAsync(MessageEnvelope envelope, string? traceParent, CancellationToken cancellationToken);
}
