using System.Net.Http.Json;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;
using LogistiFlow.BuildingBlocks.Messaging.Consuming;
using LogistiFlow.BuildingBlocks.Messaging.Enums;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Contracts.IntegrationEvents.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Orders;

/// <summary>
/// Events from different services can arrive late, early or twice. Driven by hand against the isolated Orders
/// instance, whose own relay and consumer are off.
/// </summary>
public sealed class OrderEventOrderingTests(LogistiFlowEnvironment environment)
{
    private IntegrationEventDispatcher Dispatcher => environment.IsolatedOrders.Services.GetRequiredService<IntegrationEventDispatcher>();

    [Fact]
    public async Task ReservationArrivingAfterCancellation_IsIgnored()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var order = await CreateOrderAsync(client);
        (await client.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", new { reason = "Customer changed their mind" })).EnsureSuccessStatusCode();

        var outcome = await Dispatcher.DispatchAsync(Reserved(order), 1, CancellationToken.None);

        outcome.Should().Be(DispatchOutcome.Handled, "the event is consumed (recorded in the inbox) but changes nothing");
        var current = await (await client.GetAsync($"/api/orders/{order.Id}")).ReadDataAsync();
        current.GetProperty("status").GetString().Should().Be("Cancelled");
        var history = await (await client.GetAsync($"/api/orders/{order.Id}/history")).ReadDataAsync();
        history.EnumerateArray().Select(change => change.GetProperty("toStatus").GetString()).Should().NotContain("Reserved");
    }

    [Fact]
    public async Task FulfillmentStartedBeforeReservation_IsRetriedUntilTheReservationArrives()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var order = await CreateOrderAsync(client);
        var started = Envelope(new FulfillmentStartedEvent(Guid.NewGuid(), order.Id, order.Number, "WH-NJ1", DateTimeOffset.UtcNow));

        var early = () => Dispatcher.DispatchAsync(started, 1, CancellationToken.None);
        await early.Should().ThrowAsync<TransientIntegrationEventException>("the order is not Reserved yet");

        (await Dispatcher.DispatchAsync(Reserved(order), 1, CancellationToken.None)).Should().Be(DispatchOutcome.Handled);

        // The failed attempt rolled back its inbox row, so the redelivery is processed normally.
        (await Dispatcher.DispatchAsync(started, 2, CancellationToken.None)).Should().Be(DispatchOutcome.Handled);
        var current = await (await client.GetAsync($"/api/orders/{order.Id}")).ReadDataAsync();
        current.GetProperty("status").GetString().Should().Be("Picking");
    }

    [Fact]
    public async Task DuplicateReservationEvent_IsSkippedByTheInbox()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var order = await CreateOrderAsync(client);
        var reserved = Reserved(order);

        (await Dispatcher.DispatchAsync(reserved, 1, CancellationToken.None)).Should().Be(DispatchOutcome.Handled);
        (await Dispatcher.DispatchAsync(reserved, 2, CancellationToken.None)).Should().Be(DispatchOutcome.Duplicate);

        await using var db = await environment.OpenAsync(Databases.IsolatedOrders);
        (await CountAsync(db, """
            SELECT count(*) FROM orders.order_status_changes c JOIN orders.orders o ON o.id = c.order_id
            WHERE o.public_id = @id AND c.to_status = 'Reserved'
            """, new { id = order.Id })).Should().Be(1);
    }

    [Fact]
    public async Task CancelAfterPickingStarted_IsRejectedWith409()
    {
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var order = await CreateOrderAsync(client);
        await Dispatcher.DispatchAsync(Reserved(order), 1, CancellationToken.None);
        await Dispatcher.DispatchAsync(Envelope(new FulfillmentStartedEvent(Guid.NewGuid(), order.Id, order.Number, "WH-NJ1", DateTimeOffset.UtcNow)), 1, CancellationToken.None);

        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", new { reason = "Too late" });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        (await response.ReadErrorCodeAsync()).Should().Be("order.invalid_transition");
    }

    private static async Task<(Guid Id, string Number)> CreateOrderAsync(HttpClient client)
    {
        var data = await (await client.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 1), $"ooo-{Guid.NewGuid():N}")).ReadDataAsync();
        return (data.GetProperty("id").GetGuid(), data.GetProperty("orderNumber").GetString()!);
    }

    private static MessageEnvelope Reserved((Guid Id, string Number) order) =>
        Envelope(new InventoryReservedEvent(Guid.NewGuid(), order.Id, order.Number, "WH-NJ1", [new OrderLineItem("SKU-1001", 1)], DateTimeOffset.UtcNow));

    private static MessageEnvelope Envelope<TEvent>(TEvent payload)
        where TEvent : IIntegrationEvent =>
        new(
            Guid.CreateVersion7(),
            TEvent.EventType,
            TEvent.Version,
            DateTimeOffset.UtcNow,
            "ordering-test",
            null,
            JsonSerializer.SerializeToElement(payload, MessagingJson.Options));
}
