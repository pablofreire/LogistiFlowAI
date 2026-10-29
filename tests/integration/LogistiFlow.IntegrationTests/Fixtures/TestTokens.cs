using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LogistiFlow.IntegrationTests.Fixtures;

/// <summary>Issues tokens the way the gateway does, signed with the test key.</summary>
public static class TestTokens
{
    public const string SigningKey = "integration-tests-signing-key-0123456789abcdef";

    public static string For(string role, TimeSpan? lifetime = null, string signingKey = SigningKey)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "logistiflow-gateway",
            Audience = "logistiflow-api",
            IssuedAt = now.AddMinutes(-5),
            NotBefore = now.AddMinutes(-5),
            Expires = now.Add(lifetime ?? TimeSpan.FromHours(1)),
            Subject = new ClaimsIdentity([new Claim("sub", "integration-tests"), new Claim("name", "Integration Tests"), new Claim("role", role)]),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
