using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;

/// <summary>
/// Handles one integration event type. The dispatcher runs the handler inside a transaction together with
/// the inbox record, so a redelivered event is never applied twice.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken);
}
