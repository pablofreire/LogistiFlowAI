using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Sessions;

/// <summary>
/// <see cref="IUnitOfWork"/> over the scoped <see cref="DbSession"/>. A nested call joins the outer transaction,
/// so a use case invoked from a message handler commits together with the handler's inbox record.
/// </summary>
public sealed class DbUnitOfWork(DbSession session) : IUnitOfWork
{
    public async Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (session.Transaction is not null)
        {
            return await work(cancellationToken);
        }

        await session.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await work(cancellationToken);
            if (result.IsSuccess)
            {
                await session.CommitAsync(cancellationToken);
            }
            else
            {
                await session.RollbackAsync();
            }

            return result;
        }
        catch
        {
            await session.RollbackAsync();
            throw;
        }
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (session.Transaction is not null)
        {
            await work(cancellationToken);
            return;
        }

        await session.BeginTransactionAsync(cancellationToken);
        try
        {
            await work(cancellationToken);
            await session.CommitAsync(cancellationToken);
        }
        catch
        {
            await session.RollbackAsync();
            throw;
        }
    }
}
