using System.Globalization;
using System.Net;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Web.Security;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Fixtures;

/// <summary>Business steps shared by the Phase 2 tests: create an order, drive it through the floor, follow the shipment.</summary>
public sealed class FlowSteps(LogistiFlowEnvironment environment) : IDisposable
{
    private readonly HttpClient _orders = environment.Orders.CreateClientAs(LogistiFlowRoles.OpsManager);
    private readonly HttpClient _floor = environment.Fulfillment.CreateClientAs(LogistiFlowRoles.WarehouseOperator);
    private readonly HttpClient _shipping = environment.Shipping.CreateClientAs(LogistiFlowRoles.Viewer);

    /// <summary>A promised date far enough ahead that the normal carrier estimate never counts as late.</summary>
    public static string ComfortablePromisedDate =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public HttpClient Orders => _orders;

    public HttpClient Floor => _floor;

    public HttpClient Shipping => _shipping;

    public async Task<Guid> CreateOrderAsync(string sku, int quantity, string city = "Austin", string country = "US", string? promised = null)
    {
        var created = await _orders.PostOrderAsync(
            ApiJson.OrderBody(sku, quantity, city: city, country: country, promisedDeliveryDate: promised ?? ComfortablePromisedDate),
            $"p2-{Guid.NewGuid():N}");
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await created.ReadDataAsync()).GetProperty("id").GetGuid();
    }

    public Task<JsonElement> WaitForOrderAsync(Guid orderId, string status, TimeSpan? timeout = null) =>
        EventuallyAsync(
            async () => await (await _orders.GetAsync($"/api/orders/{orderId}")).ReadDataAsync(),
            order => order.GetProperty("status").GetString() == status,
            $"order {orderId} to reach {status}",
            timeout);

    public async Task<Guid> JobIdAsync(Guid orderId)
    {
        await WaitForOrderAsync(orderId, "Picking");
        var job = await (await _floor.GetAsync($"/api/fulfillment/jobs/by-order/{orderId}")).ReadDataAsync();
        return job.GetProperty("id").GetGuid();
    }

    public async Task<HttpResponseMessage> ActAsync(Guid jobId, string action, object? body = null) =>
        await _floor.PostAsync(
            new Uri($"/api/fulfillment/jobs/{jobId}/{action}", UriKind.Relative),
            body is null ? null : System.Net.Http.Json.JsonContent.Create(body));

    /// <summary>Creates the order and takes it through pick, pack and ship; returns the order id.</summary>
    public async Task<Guid> ShipOrderAsync(string sku, int quantity, string city = "Austin", string country = "US", string? promised = null)
    {
        var orderId = await CreateOrderAsync(sku, quantity, city, country, promised);
        var jobId = await JobIdAsync(orderId);
        (await ActAsync(jobId, "pack")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ActAsync(jobId, "ship")).StatusCode.Should().Be(HttpStatusCode.OK);
        return orderId;
    }

    public Task<JsonElement> WaitForShipmentAsync(Guid orderId, Func<JsonElement, bool> condition, string description, TimeSpan? timeout = null) =>
        EventuallyAsync(
            async () =>
            {
                var response = await _shipping.GetAsync($"/api/shipping/shipments/by-order/{orderId}");
                return response.StatusCode == HttpStatusCode.OK ? await response.ReadDataAsync() : default;
            },
            shipment => shipment.ValueKind == JsonValueKind.Object && condition(shipment),
            description,
            timeout ?? TimeSpan.FromSeconds(45));

    public static bool HasStatus(JsonElement shipment, string status) => shipment.GetProperty("status").GetString() == status;

    public static IEnumerable<JsonElement> Exceptions(JsonElement shipment, string type) =>
        shipment.GetProperty("exceptions").EnumerateArray().Where(exception => exception.GetProperty("type").GetString() == type);

    public void Dispose()
    {
        _orders.Dispose();
        _floor.Dispose();
        _shipping.Dispose();
    }
}
