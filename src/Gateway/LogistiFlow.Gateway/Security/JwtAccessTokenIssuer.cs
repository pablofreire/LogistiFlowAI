using System.Security.Claims;
using System.Text;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Options;
using LogistiFlow.Gateway.Domain.Entities;
using LogistiFlow.Gateway.Interfaces;
using LogistiFlow.Gateway.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LogistiFlow.Gateway.Security;

/// <summary>Issues short-lived HS256 access tokens carrying the user's public id, name and role.</summary>
public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Issue(UserEntity user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(settings.AccessTokenLifetime);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.PublicId.ToString()),
                new Claim(JwtAuthenticationServiceCollectionExtensions.NameClaimType, user.DisplayName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtAuthenticationServiceCollectionExtensions.RoleClaimType, user.Role),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
