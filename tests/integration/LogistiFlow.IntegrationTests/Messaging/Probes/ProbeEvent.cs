using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.IntegrationTests.Messaging.Probes;

/// <summary>Test-only event bound to nothing but the probe consumer, so failures never leak into real queues.</summary>
public sealed record ProbeEvent(string Text) : IIntegrationEvent
{
    public static string EventType => "tests.probe";

    public static int Version => 1;
}
