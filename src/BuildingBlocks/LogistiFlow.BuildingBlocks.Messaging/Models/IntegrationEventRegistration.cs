using LogistiFlow.BuildingBlocks.Application.Messaging.Models;

namespace LogistiFlow.BuildingBlocks.Messaging.Models;

/// <summary>Binds an event type and version to a typed handler invocation.</summary>
public sealed record IntegrationEventRegistration(
    string EventType,
    int Version,
    Type EventClrType,
    Func<IServiceProvider, object, IntegrationEventContext, CancellationToken, Task> Invoke)
{
    public string RoutingKey => RoutingKeys.For(EventType, Version);
}
