using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.Orders.UnitTests.Fakes;

/// <summary>Runs the work inline and records whether it would have committed.</summary>
internal sealed class PassThroughUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public int Rollbacks { get; private set; }

    public async Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken)
    {
        var result = await work(cancellationToken);
        if (result.IsSuccess)
        {
            Commits++;
        }
        else
        {
            Rollbacks++;
        }

        return result;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        await work(cancellationToken);
        Commits++;
    }
}
