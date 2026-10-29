using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.IntegrationTests.Fixtures;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Orders;

/// <summary>HTTP contract of the Orders API against a real database: idempotency, validation and authorization.</summary>
public sealed class OrdersApiTests(LogistiFlowEnvironment environment)
{
    [Fact]
    public async Task CreateOrder_SameKeyTwice_ReturnsTheSameOrderAndStoresOneEvent()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var key = $"idem-{Guid.NewGuid():N}";
        var body = ApiJson.OrderBody("SKU-1001", 2);

        var first = await client.PostOrderAsync(body, key);
        var second = await client.PostOrderAsync(body, key);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Headers.GetValues("Idempotent-Replayed").Should().Equal("true");
        var firstId = (await first.ReadDataAsync()).GetProperty("id").GetGuid();
        (await second.ReadDataAsync()).GetProperty("id").GetGuid().Should().Be(firstId);

        await using var db = await environment.OpenAsync(Databases.IsolatedOrders);
        (await CountAsync(db, "SELECT count(*) FROM orders.orders WHERE idempotency_key = @key", new { key })).Should().Be(1);
        (await CountAsync(db, "SELECT count(*) FROM messaging.outbox_messages WHERE payload->>'orderId' = @id", new { id = firstId.ToString() }))
            .Should().Be(1, "the replay must not publish a second OrderCreated");
    }

    [Fact]
    public async Task CreateOrder_ConcurrentDuplicates_CreateExactlyOneOrder()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var key = $"race-{Guid.NewGuid():N}";
        var body = ApiJson.OrderBody("SKU-1001", 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.PostOrderAsync(body, key)));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(9);
        var ids = await Task.WhenAll(responses.Select(async response => (await response.ReadDataAsync()).GetProperty("id").GetGuid()));
        ids.Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task CreateOrder_SameKeyDifferentBody_Returns409()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var key = $"reuse-{Guid.NewGuid():N}";
        (await client.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 1), key)).StatusCode.Should().Be(HttpStatusCode.Created);

        var reused = await client.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 5), key);

        reused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await reused.ReadErrorCodeAsync()).Should().Be("order.idempotency_key_reused");
    }

    [Fact]
    public async Task CreateOrder_WithoutIdempotencyKey_Returns400()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);

        var response = await client.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorCodeAsync()).Should().Be("request.idempotency_key_required");
    }

    [Fact]
    public async Task CreateOrder_InvalidBody_Returns400WithFieldDetails()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);

        var response = await client.PostOrderAsync(ApiJson.OrderBody("SKU 1001", 0), $"bad-{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.ReadEnvelopeAsync()).GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("request.invalid");
        error.GetProperty("details").EnumerateObject().Select(property => property.Name)
            .Should().Contain(name => name.Contains("Sku", StringComparison.Ordinal))
            .And.Contain(name => name.Contains("Quantity", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Endpoints_WithoutToken_Return401()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(role: null);

        (await client.GetAsync("/api/orders")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 1), "anon")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Endpoints_WithForgedOrExpiredToken_Return401()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(role: null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.For(LogistiFlowRoles.Admin, signingKey: new string('x', 48)));
        (await client.GetAsync("/api/orders")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.For(LogistiFlowRoles.Admin, lifetime: TimeSpan.FromMinutes(-2)));
        (await client.GetAsync("/api/orders")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Viewer_CanReadButCannotCreateOrCancel()
    {
        using var viewer = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.Viewer);

        (await viewer.GetAsync("/api/orders")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await viewer.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 1), $"viewer-{Guid.NewGuid():N}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await viewer.PostAsJsonAsync($"/api/orders/{Guid.NewGuid()}/cancel", new { reason = "not allowed" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HealthEndpoints_AreAnonymous()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(role: null);

        (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListOrders_FiltersByStatusAndSearch()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var customer = $"Search Customer {Guid.NewGuid():N}";
        await client.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 1, customer), $"list-{Guid.NewGuid():N}");

        var response = await client.GetAsync($"/api/orders?status=PendingInventory&search={Uri.EscapeDataString(customer)}&pageSize=5");

        var data = await response.ReadDataAsync();
        data.GetProperty("totalCount").GetInt64().Should().Be(1);
        data.GetProperty("items")[0].GetProperty("customerName").GetString().Should().Be(customer);
    }
}
