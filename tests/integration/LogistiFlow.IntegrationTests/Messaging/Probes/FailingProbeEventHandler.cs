using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;

namespace LogistiFlow.IntegrationTests.Messaging.Probes;

/// <summary>Always fails, recording every attempt it saw.</summary>
public sealed class FailingProbeEventHandler(ProbeAttempts attempts) : IIntegrationEventHandler<ProbeEvent>
{
    public Task HandleAsync(ProbeEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        attempts.Record(context.Attempt);
        throw new InvalidOperationException("probe failure");
    }
}
