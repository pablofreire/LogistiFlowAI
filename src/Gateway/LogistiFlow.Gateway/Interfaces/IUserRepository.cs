using LogistiFlow.Gateway.Domain.Entities;

namespace LogistiFlow.Gateway.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken);
}
