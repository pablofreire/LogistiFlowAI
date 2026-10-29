using LogistiFlow.Gateway.Domain.Entities;
using LogistiFlow.Gateway.Models;

namespace LogistiFlow.Gateway.Interfaces;

public interface IAccessTokenIssuer
{
    AccessToken Issue(UserEntity user);
}
