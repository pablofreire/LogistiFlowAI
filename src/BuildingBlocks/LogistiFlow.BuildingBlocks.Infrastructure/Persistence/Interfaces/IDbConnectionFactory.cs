using Npgsql;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;

/// <summary>
/// Opens pooled PostgreSQL connections. Returns <see cref="NpgsqlConnection"/> rather than
/// <c>DbConnection</c> because bulk paths use PostgreSQL-specific features such as binary COPY.
/// </summary>
public interface IDbConnectionFactory
{
    ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
