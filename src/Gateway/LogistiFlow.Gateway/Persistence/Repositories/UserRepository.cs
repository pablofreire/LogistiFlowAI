using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Gateway.Domain.Entities;
using LogistiFlow.Gateway.Interfaces;
using LogistiFlow.Gateway.Persistence.Rows;

namespace LogistiFlow.Gateway.Persistence.Repositories;

public sealed class UserRepository(IDbSession session) : IUserRepository
{
    private const string SelectByEmail = """
        SELECT id, public_id, email, display_name, role, password_hash, is_active, created_at, updated_at
        FROM identity.users
        WHERE lower(email) = lower(@Email) AND is_active
        """;

    public async Task<UserEntity?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(SelectByEmail, new { Email = email }, session.Transaction, cancellationToken: cancellationToken));
        return row is null
            ? null
            : UserEntity.Restore(row.Id, row.PublicId, row.Email, row.DisplayName, row.Role, row.PasswordHash, row.IsActive, row.CreatedAt, row.UpdatedAt);
    }
}
