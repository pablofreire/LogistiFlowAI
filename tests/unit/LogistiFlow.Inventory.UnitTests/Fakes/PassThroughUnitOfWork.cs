using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.Inventory.UnitTests.Fakes;

/// <summary>Runs the work inline; transactional behaviour is covered by the integration tests.</summary>
internal sealed class PassThroughUnitOfWork : IUnitOfWork
{
    public Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken) => work(cancellationToken);

    public Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) => work(cancellationToken);
}
