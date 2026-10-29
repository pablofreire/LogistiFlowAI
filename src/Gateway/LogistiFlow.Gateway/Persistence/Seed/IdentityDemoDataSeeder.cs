using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Gateway.Interfaces;
using LogistiFlow.Gateway.Options;
using Microsoft.Extensions.Options;

namespace LogistiFlow.Gateway.Persistence.Seed;

/// <summary>
/// One demo user per role. The password comes from configuration (<c>DemoSeed:Password</c>), never from source,
/// and must be at least 12 characters. Existing users are left untouched.
/// </summary>
public sealed class IdentityDemoDataSeeder(
    IDbConnectionFactory connectionFactory,
    IPasswordHasher passwordHasher,
    IOptions<DemoSeedOptions> options) : IDemoDataSeeder
{
    public static readonly IReadOnlyList<(string Email, string Name, string Role)> Users =
    [
        ("admin@logistiflow.local", "Avery Admin", LogistiFlowRoles.Admin),
        ("ops.manager@logistiflow.local", "Morgan Ops", LogistiFlowRoles.OpsManager),
        ("warehouse.operator@logistiflow.local", "Riley Floor", LogistiFlowRoles.WarehouseOperator),
        ("viewer@logistiflow.local", "Casey Viewer", LogistiFlowRoles.Viewer),
    ];

    private const string Insert = """
        INSERT INTO identity.users (public_id, email, display_name, role, password_hash, is_active, created_at, updated_at)
        VALUES (@PublicId, @Email, @DisplayName, @Role, @PasswordHash, true, now(), now())
        ON CONFLICT (lower(email)) DO NOTHING
        """;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var password = options.Value.Password;
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new InvalidOperationException("DemoSeed:Password must be set to at least 12 characters to seed demo users.");
        }

        var hash = passwordHasher.Hash(password);
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        foreach (var (email, name, role) in Users)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                Insert,
                new { PublicId = Guid.CreateVersion7(), Email = email, DisplayName = name, Role = role, PasswordHash = hash },
                cancellationToken: cancellationToken));
        }
    }
}
