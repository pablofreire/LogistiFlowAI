namespace LogistiFlow.Contracts.IntegrationEvents.Interfaces;

/// <summary>
/// A fact published by one service and consumed by others. Contracts are additive-only within a version;
/// a breaking change introduces a new record with a higher <see cref="Version"/>.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Stable, dot-separated name such as <c>orders.order-created</c>.</summary>
    static abstract string EventType { get; }

    static abstract int Version { get; }
}
