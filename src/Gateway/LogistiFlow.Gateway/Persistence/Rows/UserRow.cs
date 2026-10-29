namespace LogistiFlow.Gateway.Persistence.Rows;

internal sealed class UserRow
{
    public long Id { get; init; }

    public Guid PublicId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
