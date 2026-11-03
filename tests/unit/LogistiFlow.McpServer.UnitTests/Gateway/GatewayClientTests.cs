using System.Net;
using LogistiFlow.McpServer.Enums;
using LogistiFlow.McpServer.Gateway;
using LogistiFlow.McpServer.UnitTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogistiFlow.McpServer.UnitTests.Gateway;

/// <summary>Status codes and transport failures become outcomes with messages, never exceptions or invented data.</summary>
public sealed class GatewayClientTests : IDisposable
{
    private readonly FakeGateway _gateway = new();
    private readonly HttpClient _http;

    public GatewayClientTests() =>
        _http = new HttpClient(_gateway.CreateHandler()) { BaseAddress = new Uri("http://gateway.test/") };

    public void Dispose() => _http.Dispose();

    private GatewayClient Client() => new(_http, NullLogger<GatewayClient>.Instance);

    [Fact]
    public async Task UnreachableGateway_IsUnavailable()
    {
        _gateway.Throws("/api/metrics/inventory", new HttpRequestException("Connection refused"));

        var reply = await Client().GetInventoryMetricsAsync(TestContext.Current.CancellationToken);

        reply.Outcome.Should().Be(GatewayOutcome.Unavailable);
        reply.Message.Should().Be("The LogistiFlow gateway is unavailable right now.");
    }

    [Fact]
    public async Task ServerError_IsUnavailable_WithTheStatusCode()
    {
        _gateway.Fail("/api/metrics/inventory", HttpStatusCode.BadGateway, "Bad gateway.");

        var reply = await Client().GetInventoryMetricsAsync(TestContext.Current.CancellationToken);

        reply.Outcome.Should().Be(GatewayOutcome.Unavailable);
        reply.Message.Should().Contain("HTTP 502");
    }

    [Fact]
    public async Task NotFound_CarriesTheServiceMessage()
    {
        _gateway.Fail("/api/inventory/availability/SKU-0000", HttpStatusCode.NotFound, "SKU SKU-0000 does not exist.");

        var reply = await Client().GetAvailabilityAsync("SKU-0000", TestContext.Current.CancellationToken);

        reply.Outcome.Should().Be(GatewayOutcome.NotFound);
        reply.Message.Should().Be("SKU SKU-0000 does not exist.");
    }

    [Fact]
    public async Task BadRequest_IsInvalid_WithTheServiceMessage()
    {
        _gateway.Fail("/api/shipping/exceptions", HttpStatusCode.BadRequest, "pageSize must be at most 100.");

        var reply = await Client().ListExceptionsAsync(null, null, 500, TestContext.Current.CancellationToken);

        reply.Outcome.Should().Be(GatewayOutcome.Invalid);
        reply.Message.Should().Be("pageSize must be at most 100.");
    }

    [Fact]
    public async Task OrderSearch_MatchesTheExactNumber_NotAPrefix()
    {
        _gateway.Ok("/api/orders", new { items = new[] { new { id = Guid.NewGuid(), orderNumber = "ORD-10250", status = "Created" } }, totalCount = 1 });

        var reply = await Client().FindOrderAsync("ORD-1025", TestContext.Current.CancellationToken);

        reply.Outcome.Should().Be(GatewayOutcome.NotFound);
        _gateway.Requests.Should().ContainSingle();
    }
}
