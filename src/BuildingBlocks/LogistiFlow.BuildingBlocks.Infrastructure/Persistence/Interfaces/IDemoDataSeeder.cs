namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;

/// <summary>
/// Inserts deterministic demo data for local runs and end-to-end tests. Must be idempotent. Runs only when a
/// host is started with <c>--seed-demo</c>, never as part of schema migrations.
/// </summary>
public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
