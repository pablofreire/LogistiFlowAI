using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;

/// <summary>
/// Runs a unit of work inside one database transaction. Repository writes and outbox messages enqueued
/// inside the delegate commit together, or not at all.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Commits when the returned result is a success; rolls back on failure or exception.</summary>
    Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken);

    /// <summary>Commits when the delegate completes; rolls back on exception.</summary>
    Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken);
}
