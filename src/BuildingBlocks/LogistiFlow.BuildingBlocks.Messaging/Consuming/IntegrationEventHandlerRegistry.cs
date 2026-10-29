using System.Text.Json;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.Contracts.IntegrationEvents.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.BuildingBlocks.Messaging.Consuming;

/// <summary>The event types a service consumes and the handler of each one.</summary>
public sealed class IntegrationEventHandlerRegistry
{
    private readonly Dictionary<string, IntegrationEventRegistration> _registrations = new(StringComparer.Ordinal);
    private readonly IServiceCollection _services;

    public IntegrationEventHandlerRegistry(IServiceCollection services)
    {
        _services = services;
    }

    public IReadOnlyCollection<IntegrationEventRegistration> Registrations => _registrations.Values;

    public IntegrationEventHandlerRegistry Add<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        _services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();

        var registration = new IntegrationEventRegistration(
            TEvent.EventType,
            TEvent.Version,
            typeof(TEvent),
            static (provider, payload, context, cancellationToken) =>
                provider.GetRequiredService<IIntegrationEventHandler<TEvent>>().HandleAsync((TEvent)payload, context, cancellationToken));

        _registrations[registration.RoutingKey] = registration;
        return this;
    }

    public bool TryGet(string eventType, int version, out IntegrationEventRegistration registration) =>
        _registrations.TryGetValue(RoutingKeys.For(eventType, version), out registration!);

    internal static object Deserialize(JsonElement payload, Type eventType) =>
        payload.Deserialize(eventType, MessagingJson.Options)
        ?? throw new JsonException($"Payload of {eventType.Name} deserialized to null.");
}
