using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogistiFlow.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Readiness probe: succeeds when a pooled connection can be opened and a trivial query executes.
/// </summary>
public sealed class PostgresHealthCheck(IDbConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
#pragma warning disable CA1031 // A health check reports every failure instead of throwing.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable.", exception);
        }
    }
}
