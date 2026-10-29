using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;

/// <summary>
/// Transactional outbox. Call it inside <see cref="Persistence.Interfaces.IUnitOfWork"/> so the event is stored
/// in the same transaction as the state change that produced it; a relay publishes it to the broker afterwards.
/// </summary>
public interface IIntegrationEventOutbox
{
    Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}
