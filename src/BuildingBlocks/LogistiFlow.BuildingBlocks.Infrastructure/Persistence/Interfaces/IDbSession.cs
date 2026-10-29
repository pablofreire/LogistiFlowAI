using Npgsql;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;

/// <summary>
/// The connection and (optional) transaction shared by every repository in one DI scope. Repositories pass
/// <see cref="Transaction"/> to Dapper so their writes join the unit of work started by the use case.
/// </summary>
public interface IDbSession
{
    NpgsqlTransaction? Transaction { get; }

    ValueTask<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
