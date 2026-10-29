using LogistiFlow.BuildingBlocks.Messaging.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogistiFlow.BuildingBlocks.Messaging.HealthChecks;

/// <summary>
/// Reports broker connectivity as <see cref="HealthStatus.Degraded"/>, not unhealthy: thanks to the outbox the
/// service keeps accepting requests while RabbitMQ is down and catches up when it returns.
/// </summary>
public sealed class RabbitMqHealthCheck(IRabbitMqConnectionProvider connectionProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded("RabbitMQ connection is recovering.");
        }
#pragma warning disable CA1031 // A health check reports every failure instead of throwing.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Degraded("RabbitMQ is unreachable; events stay in the outbox.", exception);
        }
    }
}
