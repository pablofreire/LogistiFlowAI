using Microsoft.Extensions.Configuration;

namespace LogistiFlow.McpServer.Options;

/// <summary>
/// Where the gateway is and who the server signs in as. Read only from environment variables (or another
/// configuration source the host adds), never from a committed file. Either a pre-issued access token or an e-mail and
/// password is required; the platform's own role policies decide what that user may read.
/// </summary>
public sealed class GatewayOptions
{
    public const string BaseUrlVariable = "LOGISTIFLOW_GATEWAY_URL";
    public const string AccessTokenVariable = "LOGISTIFLOW_ACCESS_TOKEN";
    public const string EmailVariable = "LOGISTIFLOW_EMAIL";
    public const string PasswordVariable = "LOGISTIFLOW_PASSWORD";

    /// <summary>The local Docker Compose gateway.</summary>
    public static readonly Uri DefaultBaseUrl = new("http://localhost:8089/");

    public Uri BaseUrl { get; set; } = DefaultBaseUrl;

    public string? AccessToken { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public bool UsesAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

    public bool HasCredentials => UsesAccessToken || (!string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password));

    public bool HasHttpBaseUrl => BaseUrl.IsAbsoluteUri && (BaseUrl.Scheme == Uri.UriSchemeHttp || BaseUrl.Scheme == Uri.UriSchemeHttps);

    public static void Bind(IConfiguration configuration, GatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);

        if (Uri.TryCreate(configuration[BaseUrlVariable], UriKind.Absolute, out var baseUrl))
        {
            // A trailing slash keeps relative paths under the base address.
            options.BaseUrl = baseUrl.AbsoluteUri.EndsWith('/') ? baseUrl : new Uri(baseUrl.AbsoluteUri + "/");
        }

        options.AccessToken = configuration[AccessTokenVariable];
        options.Email = configuration[EmailVariable];
        options.Password = configuration[PasswordVariable];
    }
}
