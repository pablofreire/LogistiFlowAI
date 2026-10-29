using System.Text.Json;
using LogistiFlow.CarrierSimulator.Booking;
using LogistiFlow.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.IntegrationTests.Shipping;

/// <summary>
/// Each deterministic carrier scenario over real HTTP (the simulator's test server): AC-23, AC-24, AC-25.
/// The simulator runs one tracking step every 200 ms; the client timeout is 750 ms and the slow answer takes 3 s.
/// </summary>
public sealed class CarrierScenarioTests(LogistiFlowEnvironment environment) : IDisposable
{
    private readonly FlowSteps _steps = new(environment);

    private BookingStore Carrier => environment.CarrierSimulator.Services.GetRequiredService<BookingStore>();

    [Fact]
    public async Task Delay_OpensADelayException_ThatDeliveryResolves()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Delay");

        var delayed = await _steps.WaitForShipmentAsync(orderId, s => s.GetProperty("isDelayed").GetBoolean(), "the delay to be detected");
        var delay = FlowSteps.Exceptions(delayed, "Delay").Should().ContainSingle().Subject;
        delay.GetProperty("status").GetString().Should().Be("Open");
        delay.GetProperty("details").GetString().Should().Contain("severe weather").And.Contain("Estimate moved to");
        delayed.GetProperty("estimatedDelivery").GetString().Should().NotBe(delayed.GetProperty("originalEstimatedDelivery").GetString());

        using var shipping = environment.Shipping.CreateClientAs("Viewer");
        var delayedList = await (await shipping.GetAsync("/api/shipping/shipments?delayedOnly=true&pageSize=100")).ReadDataAsync();
        delayedList.GetProperty("items").EnumerateArray().Should().Contain(item => item.GetProperty("orderId").GetGuid() == orderId);

        var delivered = await _steps.WaitForShipmentAsync(orderId, s => FlowSteps.HasStatus(s, "Delivered"), "the delayed shipment to be delivered");
        delivered.GetProperty("isDelayed").GetBoolean().Should().BeFalse();
        FlowSteps.Exceptions(delivered, "Delay").Single().GetProperty("status").GetString().Should().Be("Resolved");
    }

    [Fact]
    public async Task TemporaryFailure_IsRetriedWithBackoff_AndSurfacedThenResolved()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Unavailable");

        var booked = await _steps.WaitForShipmentAsync(orderId, s => s.GetProperty("trackingNumber").ValueKind == JsonValueKind.String, "the booking to succeed");

        booked.GetProperty("bookingAttempts").GetInt32().Should().Be(3);
        Carrier.AttemptsFor(booked.GetProperty("id").GetGuid().ToString()).Should().Be(3);
        var carrierException = FlowSteps.Exceptions(booked, "CarrierUnavailable").Should().ContainSingle().Subject;
        carrierException.GetProperty("status").GetString().Should().Be("Resolved");
        carrierException.GetProperty("details").GetString().Should().Contain("HTTP 503");
    }

    [Fact]
    public async Task Timeout_IsRetriedWithTheSameIdempotencyKey_AndBooksExactlyOnce()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Timeout");

        var booked = await _steps.WaitForShipmentAsync(orderId, s => s.GetProperty("trackingNumber").ValueKind == JsonValueKind.String, "the booking after the timeout");

        var orderNumber = booked.GetProperty("orderNumber").GetString()!;
        Carrier.BookingsForReference(orderNumber).Should().Be(1, "the retry must reuse the booking the timed-out call created");
        Carrier.AttemptsFor(booked.GetProperty("id").GetGuid().ToString()).Should().BeGreaterThanOrEqualTo(2);
        FlowSteps.Exceptions(booked, "CarrierUnavailable").Should().ContainSingle()
            .Which.GetProperty("details").GetString().Should().Contain("did not answer in time");
    }

    [Fact]
    public async Task Redelivery_EndsDelivered_AfterAFailedAttempt()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Redelivery");

        var delivered = await _steps.WaitForShipmentAsync(orderId, s => FlowSteps.HasStatus(s, "Delivered"), "the second attempt to deliver");

        delivered.GetProperty("trackingEvents").EnumerateArray().Count(e => e.GetProperty("code").GetString() == "OUT_FOR_DELIVERY").Should().Be(2);
        FlowSteps.Exceptions(delivered, "DeliveryAttemptFailed").Should().ContainSingle()
            .Which.GetProperty("status").GetString().Should().Be("Resolved");
        await _steps.WaitForOrderAsync(orderId, "Delivered");
    }

    [Fact]
    public async Task AddressNotFound_IsReturnedToSender_AndFailsTheOrder()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Address Not Found");

        var returned = await _steps.WaitForShipmentAsync(orderId, s => FlowSteps.HasStatus(s, "ReturnedToSender"), "the return to sender");

        FlowSteps.Exceptions(returned, "ReturnedToSender").Should().ContainSingle()
            .Which.GetProperty("status").GetString().Should().Be("Open");
        var order = await _steps.WaitForOrderAsync(orderId, "Failed");
        order.GetProperty("statusReason").GetString().Should().Contain("Returned to sender").And.Contain("address not located");
    }

    public void Dispose() => _steps.Dispose();
}
