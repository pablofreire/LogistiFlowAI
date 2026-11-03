using System.Text.Json;
using LogistiFlow.McpServer.Tools;
using LogistiFlow.McpServer.UnitTests.Fixtures;
using static LogistiFlow.McpServer.UnitTests.Fixtures.McpTestServer;

namespace LogistiFlow.McpServer.UnitTests.Tools;

/// <summary>
/// Every tool's structured content must satisfy the output schema it advertises, including when optional values are
/// null. Claude Code validates this and rejects a non-conforming result; the SDK's own client does not, so this test
/// is the only guard. Regression for the first Claude Code run, where an unfiltered exception list was rejected.
/// </summary>
public sealed class OutputSchemaConformanceTests : IAsyncLifetime
{
    private readonly FakeGateway _gateway = new();
    private McpTestServer _server = null!;

    public async ValueTask InitializeAsync()
    {
        // Sample data with nulls: status reasons, the first history entry, an unresolved exception, no delivery yet.
        _gateway.Ok("/api/orders", LogisticsData.SearchPage())
            .Ok($"/api/orders/{LogisticsData.OrderId}", LogisticsData.Order())
            .Ok($"/api/orders/{LogisticsData.OrderId}/history", LogisticsData.History())
            .Ok($"/api/shipping/shipments/by-order/{LogisticsData.OrderId}", LogisticsData.Shipment())
            .Ok("/api/inventory/availability/SKU-1003", LogisticsData.Availability())
            .Ok("/api/shipping/exceptions", LogisticsData.ExceptionPage())
            .Ok("/api/metrics/fulfillment", LogisticsData.FulfillmentMetrics())
            .Ok("/api/metrics/inventory", LogisticsData.InventoryMetrics());
        _server = await StartAsync(_gateway);
    }

    public ValueTask DisposeAsync() => _server.DisposeAsync();

    public static TheoryData<string, string> Calls() => new()
    {
        { ToolNames.GetOrder, """{"orderNumber":"ORD-1025"}""" },
        { ToolNames.GetShipment, """{"orderNumber":"ORD-1025"}""" },
        { ToolNames.GetInventory, """{"sku":"SKU-1003"}""" },
        { ToolNames.ListExceptions, "{}" },
        { ToolNames.ListExceptions, """{"status":"Open","type":"Delay","limit":5}""" },
        { ToolNames.GetWarehouseMetrics, "{}" },
    };

    [Theory]
    [MemberData(nameof(Calls))]
    public async Task StructuredContent_SatisfiesTheAdvertisedOutputSchema(string tool, string argumentsJson)
    {
        var tools = await _server.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var schema = JsonSerializer.SerializeToElement(tools.Single(candidate => candidate.Name == tool).ProtocolTool.OutputSchema);
        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson)!;

        var content = Structured(await _server.CallAsync(tool, arguments));

        OutputSchema.Violations(schema, content).Should().BeEmpty();
    }

    [Fact]
    public void Checker_ReportsAMissingRequiredPropertyAndADisallowedNull()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"status":{"type":"string"},"total":{"type":"integer"}},"required":["status","total"]}""").RootElement;

        OutputSchema.Violations(schema, JsonDocument.Parse("""{"total":1}""").RootElement).Should().ContainSingle().Which.Should().Contain("'status'");
        OutputSchema.Violations(schema, JsonDocument.Parse("""{"status":null,"total":1}""").RootElement).Should().ContainSingle().Which.Should().Contain("Null");
    }
}
