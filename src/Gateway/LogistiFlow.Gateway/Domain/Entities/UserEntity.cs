using LogistiFlow.BuildingBlocks.Domain.Entities;

namespace LogistiFlow.Gateway.Domain.Entities;

/// <summary>An operator who signs in to the platform. The gateway owns identity; services only see the token.</summary>
public sealed class UserEntity : Entity
{
    private UserEntity(string email, string displayName, string role, string passwordHash)
    {
        Email = email;
        DisplayName = displayName;
        Role = role;
        PasswordHash = passwordHash;
    }

    public string Email { get; }

    public string DisplayName { get; }

    public string Role { get; }

    public string PasswordHash { get; }

    public static UserEntity Restore(long id, Guid publicId, string email, string displayName, string role, string passwordHash, bool isActive, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        var user = new UserEntity(email, displayName, role, passwordHash);
        user.Restore(id, publicId, isActive, createdAt, updatedAt);
        return user;
    }
}
