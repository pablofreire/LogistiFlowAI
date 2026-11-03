using System.Text.Json;
using System.Text.Json.Serialization;
using LogistiFlow.McpServer.Gateway;
using LogistiFlow.McpServer.Interfaces;
using LogistiFlow.McpServer.Options;
using LogistiFlow.McpServer.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace LogistiFlow.McpServer.Composition;

public static class McpServerServiceCollectionExtensions
{
    public const string ServerName = "logistiflow";

    public const string Instructions =
        "Read-only access to LogistiFlow AI, a logistics and fulfillment platform. Order numbers look like ORD-1025 and SKUs like SKU-1003. " +
        "Answer only from tool results; when a tool returns an error, say what it reported instead of guessing. These tools cannot change anything.";

    /// <summary>
    /// Tool results keep null properties. The SDK's defaults omit them, but the generated output schema lists every
    /// property as required, so a client that validates structured content (Claude Code does) rejects the result.
    /// </summary>
    public static readonly JsonSerializerOptions ToolJson = new(McpJsonUtilities.DefaultOptions) { DefaultIgnoreCondition = JsonIgnoreCondition.Never };

    /// <summary>
    /// The MCP server without a transport: <c>Program</c> adds stdio, tests add in-memory streams.
    /// <paramref name="configureHttpClient"/> lets tests replace the gateway's HTTP handler for both the API and the
    /// sign-in client.
    /// </summary>
    public static IMcpServerBuilder AddLogistiFlowMcpServer(this IServiceCollection services, IConfiguration configuration, Action<IHttpClientBuilder>? configureHttpClient = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<GatewayOptions>()
            .Configure(options => GatewayOptions.Bind(configuration, options))
            .Validate(options => options.HasHttpBaseUrl, $"{GatewayOptions.BaseUrlVariable} must be an absolute http or https URL.")
            .Validate(options => options.HasCredentials, $"Set {GatewayOptions.AccessTokenVariable}, or {GatewayOptions.EmailVariable} and {GatewayOptions.PasswordVariable}.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAccessTokenProvider, GatewayAccessTokenProvider>();
        services.AddTransient<BearerTokenHandler>();

        var api = services.AddHttpClient<IGatewayClient, GatewayClient>(ConfigureBaseAddress)
            .AddHttpMessageHandler<BearerTokenHandler>();
        api.AddStandardResilienceHandler(resilience =>
        {
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
            resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        });
        configureHttpClient?.Invoke(api);

        var signIn = services.AddHttpClient(GatewayAccessTokenProvider.SignInClientName, ConfigureBaseAddress);
        configureHttpClient?.Invoke(signIn);

        return services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = ServerName,
                    Title = "LogistiFlow AI",
                    Version = typeof(McpServerServiceCollectionExtensions).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
                };
                options.ServerInstructions = Instructions;
            })
            .WithTools<LogisticsTools>(ToolJson);
    }

    private static void ConfigureBaseAddress(IServiceProvider services, HttpClient client) =>
        client.BaseAddress = services.GetRequiredService<IOptions<GatewayOptions>>().Value.BaseUrl;
}
