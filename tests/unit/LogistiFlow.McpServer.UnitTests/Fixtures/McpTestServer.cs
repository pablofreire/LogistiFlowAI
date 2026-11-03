using System.IO.Pipelines;
using System.Text.Json;
using LogistiFlow.McpServer.Composition;
using LogistiFlow.McpServer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LogistiFlow.McpServer.UnitTests.Fixtures;

/// <summary>
/// The real server composition, hosted in-process over in-memory streams, with the SDK's own client connected to it.
/// Only the gateway's HTTP handler is replaced.
/// </summary>
public sealed class McpTestServer : IAsyncDisposable
{
    public const string AccessToken = "unit-test-token";

    private readonly IHost _host;

    private McpTestServer(IHost host, McpClient client)
    {
        _host = host;
        Client = client;
    }

    public McpClient Client { get; }

    public static Dictionary<string, string?> TokenSettings() => new(StringComparer.Ordinal)
    {
        [GatewayOptions.BaseUrlVariable] = "http://gateway.test",
        [GatewayOptions.AccessTokenVariable] = AccessToken,
    };

    public static async Task<McpTestServer> StartAsync(FakeGateway gateway, Dictionary<string, string?>? settings = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(gateway);

        var clientToServer = new Pipe();
        var serverToClient = new Pipe();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(settings ?? TokenSettings());
        if (timeProvider is not null)
        {
            builder.Services.AddSingleton(timeProvider);
        }

        builder.Services
            .AddLogistiFlowMcpServer(builder.Configuration, http => http.ConfigurePrimaryHttpMessageHandler(gateway.CreateHandler))
            .WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream());

        var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var client = await McpClient.CreateAsync(
            new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()),
            cancellationToken: TestContext.Current.CancellationToken);
        return new McpTestServer(host, client);
    }

    public async Task<CallToolResult> CallAsync(string tool, Dictionary<string, object?>? arguments = null) =>
        await Client.CallToolAsync(tool, arguments ?? [], cancellationToken: TestContext.Current.CancellationToken);

    public static JsonElement Structured(CallToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        result.IsError.Should().NotBe(true, Text(result));
        return JsonSerializer.SerializeToElement(result.StructuredContent);
    }

    public static string Text(CallToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await _host.StopAsync();
        _host.Dispose();
    }
}
