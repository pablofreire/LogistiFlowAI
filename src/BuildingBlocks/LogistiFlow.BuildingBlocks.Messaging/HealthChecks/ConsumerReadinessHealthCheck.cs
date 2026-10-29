using LogistiFlow.BuildingBlocks.Messaging.Consuming;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogistiFlow.BuildingBlocks.Messaging.HealthChecks;

/// <summary>
/// Readiness gate for consuming services (ADR-0003): until the queues exist, events published by other services would
/// be dropped for this one, so the instance reports not ready and compose and orchestrators hold traffic back.
/// </summary>
public sealed class ConsumerReadinessHealthCheck(ConsumerReadiness readiness) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(readiness.TopologyDeclared
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Consumer queues are not declared yet."));
}
