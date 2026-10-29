using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.Gateway.Controllers.Contracts;
using LogistiFlow.Gateway.Options;
using LogistiFlow.Gateway.UseCases.Auth.Login.Contracts;
using LogistiFlow.Gateway.UseCases.Auth.Login.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LogistiFlow.Gateway.Controllers;

[Route("api/auth")]
public sealed class AuthController : BaseApiController
{
    /// <summary>Exchanges e-mail and password for a bearer token. Rate limited per client IP.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitOptions.PolicyName)]
    [ProducesResponseType<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Login([FromBody] LoginBody body, [FromServices] ILoginUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new LoginRequest(body.Email, body.Password), cancellationToken));

    /// <summary>The identity carried by the caller's token.</summary>
    [HttpGet("me")]
    [ProducesResponseType<ApiResponse<AuthenticatedUser>>(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var user = new AuthenticatedUser(
            Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : Guid.Empty,
            User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
            User.FindFirst(JwtAuthenticationServiceCollectionExtensions.NameClaimType)?.Value ?? string.Empty,
            User.FindFirst(JwtAuthenticationServiceCollectionExtensions.RoleClaimType)?.Value ?? string.Empty);
        return Envelope(user, StatusCodes.Status200OK);
    }
}
