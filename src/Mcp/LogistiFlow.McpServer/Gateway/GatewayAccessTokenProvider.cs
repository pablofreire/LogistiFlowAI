using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LogistiFlow.McpServer.Contracts.Gateway;
using LogistiFlow.McpServer.Exceptions;
using LogistiFlow.McpServer.Interfaces;
using LogistiFlow.McpServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiFlow.McpServer.Gateway;

/// <summary>
/// Uses the configured access token as is, or signs in at <c>POST /api/auth/login</c> with the configured e-mail and
/// password and keeps the token until shortly before it expires. Neither the password nor the token is ever logged.
/// </summary>
public sealed partial class GatewayAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GatewayOptions> options,
    TimeProvider timeProvider,
    ILogger<GatewayAccessTokenProvider> logger) : IAccessTokenProvider, IDisposable
{
    /// <summary>The named client used for sign-in; it has no <see cref="BearerTokenHandler"/>.</summary>
    public const string SignInClientName = "logistiflow-sign-in";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan RenewBeforeExpiry = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _signIn = new(1, 1);
    private LoginResult? _current;

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.UsesAccessToken)
        {
            return settings.AccessToken!;
        }

        // Read the field once: Invalidate may clear it between the check and the use.
        var cached = _current;
        if (IsFresh(cached))
        {
            return cached!.AccessToken;
        }

        await _signIn.WaitAsync(cancellationToken);
        try
        {
            var current = _current;
            if (!IsFresh(current))
            {
                current = await SignInAsync(settings, cancellationToken);
                _current = current;
                LogSignedIn(logger, settings.Email!, current.ExpiresAt);
            }

            return current!.AccessToken;
        }
        finally
        {
            _signIn.Release();
        }
    }

    public void Invalidate() => _current = null;

    public void Dispose() => _signIn.Dispose();

    private bool IsFresh(LoginResult? token) => token is not null && token.ExpiresAt - RenewBeforeExpiry > timeProvider.GetUtcNow();

    private async Task<LoginResult> SignInAsync(GatewayOptions settings, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(SignInClientName);
        using var response = await client.PostAsJsonAsync(new Uri("api/auth/login", UriKind.Relative), new LoginBody(settings.Email!, settings.Password!), Json, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            throw new GatewaySignInException($"The gateway did not accept the configured credentials for {settings.Email}.");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new GatewaySignInException("The gateway is rate limiting sign-in; try again in a minute.");
        }

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<GatewayEnvelope<LoginResult>>(Json, cancellationToken);
        return envelope?.Data ?? throw new GatewaySignInException("The gateway returned an empty sign-in response.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Signed in to the gateway as {Email}; the token expires at {ExpiresAt}")]
    private static partial void LogSignedIn(ILogger logger, string email, DateTimeOffset expiresAt);
}
