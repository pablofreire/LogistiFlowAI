using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using Npgsql;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Sessions;

/// <summary>
/// Scoped holder of one pooled connection. The connection is opened lazily, so read-only requests that never
/// touch the database do not take a connection from the pool.
/// </summary>
public sealed class DbSession(IDbConnectionFactory connectionFactory) : IDbSession, IAsyncDisposable
{
    private NpgsqlConnection? _connection;

    public NpgsqlTransaction? Transaction { get; private set; }

    public async ValueTask<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        return _connection ??= await connectionFactory.OpenConnectionAsync(cancellationToken);
    }

    internal async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (Transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active in this session.");
        }

        var connection = await GetConnectionAsync(cancellationToken);
        Transaction = await connection.BeginTransactionAsync(cancellationToken);
    }

    internal async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (Transaction is null)
        {
            return;
        }

        await Transaction.CommitAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    internal async Task RollbackAsync()
    {
        if (Transaction is null)
        {
            return;
        }

        await Transaction.RollbackAsync(CancellationToken.None);
        await DisposeTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async ValueTask DisposeTransactionAsync()
    {
        if (Transaction is not null)
        {
            await Transaction.DisposeAsync();
            Transaction = null;
        }
    }
}
