using System.IO.Pipelines;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.IntegrationTests.Fixtures;
using LogistiFlow.McpServer.Composition;
using LogistiFlow.McpServer.Options;
using LogistiFlow.McpServer.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LogistiFlow.IntegrationTests.Mcp;

/// <summary>
/// The MCP server against the real services: the SDK client calls the tools, the server reads through the gateway's
/// routes with a Viewer token (the least-privileged role), and the answers match what the APIs return.
/// </summary>
public sealed class McpServerFlowTests(LogistiFlowEnvironment environment) : IDisposable
{
    private readonly FlowSteps _steps = new(environment);

    [Fact]
    public async Task ShippedOrder_IsReadThroughEveryTool_AsAViewer()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1);
        var shipment = await _steps.WaitForShipmentAsync(orderId, shipment => shipment.GetProperty("trackingNumber").GetString() is not null, "the shipment to be booked");
        var orderNumber = shipment.GetProperty("orderNumber").GetString()!;
        await using var mcp = await StartAsync(TestTokens.For(LogistiFlowRoles.Viewer));

        var order = await mcp.StructuredAsync(ToolNames.GetOrder, new() { ["orderNumber"] = orderNumber });
        order.GetProperty("order").GetProperty("id").GetGuid().Should().Be(orderId);
        order.GetProperty("history").EnumerateArray().Select(change => change.GetProperty("toStatus").GetString())
            .Should().ContainInOrder("Created", "Reserved", "Picking", "Shipped");

        var tracked = await mcp.StructuredAsync(ToolNames.GetShipment, new() { ["orderNumber"] = orderNumber });
        tracked.GetProperty("trackingNumber").GetString().Should().Be(shipment.GetProperty("trackingNumber").GetString());
        tracked.GetProperty("carrierCode").GetString().Should().Be(shipment.GetProperty("carrierCode").GetString());

        var stock = await mcp.StructuredAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" });
        stock.GetProperty("warehouses").EnumerateArray().Should().NotBeEmpty()
            .And.OnlyContain(warehouse => warehouse.GetProperty("available").GetInt32() == warehouse.GetProperty("onHand").GetInt32() - warehouse.GetProperty("reserved").GetInt32());

        var metrics = await mcp.StructuredAsync(ToolNames.GetWarehouseMetrics);
        metrics.GetProperty("totalJobs").GetInt64().Should().BePositive();
        metrics.GetProperty("warehouses").EnumerateArray().Select(warehouse => warehouse.GetProperty("warehouseCode").GetString())
            .Should().Contain(shipment.GetProperty("warehouseCode").GetString());
    }

    [Fact]
    public async Task CarrierDelay_IsListedByTheExceptionsTool()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Delay");
        var shipment = await _steps.WaitForShipmentAsync(orderId, shipment => FlowSteps.Exceptions(shipment, "Delay").Any(), "the carrier delay to be recorded");
        var orderNumber = shipment.GetProperty("orderNumber").GetString()!;
        await using var mcp = await StartAsync(TestTokens.For(LogistiFlowRoles.Viewer));

        var exceptions = await mcp.StructuredAsync(ToolNames.ListExceptions, new() { ["type"] = "Delay", ["limit"] = 100 });

        exceptions.GetProperty("type").GetString().Should().Be("Delay");
        exceptions.GetProperty("items").EnumerateArray()
            .Should().Contain(exception => exception.GetProperty("orderNumber").GetString() == orderNumber)
            .And.OnlyContain(exception => exception.GetProperty("type").GetString() == "Delay");
    }

    [Fact]
    public async Task UnknownOrder_IsAToolError()
    {
        await using var mcp = await StartAsync(TestTokens.For(LogistiFlowRoles.Viewer));

        var result = await mcp.CallAsync(ToolNames.GetOrder, new() { ["orderNumber"] = "ORD-9999999999" });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("Order ORD-9999999999 does not exist.");
    }

    [Fact]
    public async Task TokenTheServicesDoNotTrust_IsAToolError_NotData()
    {
        await using var mcp = await StartAsync(TestTokens.For(LogistiFlowRoles.Viewer, signingKey: Convert.ToHexString(Guid.NewGuid().ToByteArray()) + Convert.ToHexString(Guid.NewGuid().ToByteArray())));

        var result = await mcp.CallAsync(ToolNames.GetWarehouseMetrics);

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("did not accept");
    }

    private async Task<McpSession> StartAsync(string accessToken)
    {
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [GatewayOptions.BaseUrlVariable] = "http://gateway/",
            [GatewayOptions.AccessTokenVariable] = accessToken,
        });
        builder.Services
            .AddLogistiFlowMcpServer(builder.Configuration, http => http.ConfigurePrimaryHttpMessageHandler(() => new GatewayRoutingHandler(environment)))
            .WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream());
        var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        var client = await McpClient.CreateAsync(
            new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()),
            cancellationToken: TestContext.Current.CancellationToken);
        return new McpSession(host, client);
    }

    private static string Text(CallToolResult result) => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    public void Dispose() => _steps.Dispose();

    private sealed class McpSession(IHost host, McpClient client) : IAsyncDisposable
    {
        public async Task<CallToolResult> CallAsync(string tool, Dictionary<string, object?>? arguments = null) =>
            await client.CallToolAsync(tool, arguments ?? [], cancellationToken: TestContext.Current.CancellationToken);

        public async Task<JsonElement> StructuredAsync(string tool, Dictionary<string, object?>? arguments = null)
        {
            var result = await CallAsync(tool, arguments);
            result.IsError.Should().NotBe(true, Text(result));
            return JsonSerializer.SerializeToElement(result.StructuredContent);
        }

        public async ValueTask DisposeAsync()
        {
            await client.DisposeAsync();
            await host.StopAsync();
            host.Dispose();
        }
    }
}
