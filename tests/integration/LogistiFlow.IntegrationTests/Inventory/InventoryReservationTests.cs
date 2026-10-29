using System.Text.Json;
using Dapper;
using LogistiFlow.BuildingBlocks.Messaging.Consuming;
using LogistiFlow.BuildingBlocks.Messaging.Enums;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Interfaces;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Inventory;

/// <summary>Reservation guarantees proven against PostgreSQL: no overselling, idempotency and compensation.</summary>
public sealed class InventoryReservationTests(LogistiFlowEnvironment environment)
{
    [Fact]
    public async Task ConcurrentReservations_ForTheLastUnits_NeverOversell()
    {
        // SKU-LIMITED-001 has exactly 10 units, all in WH-NJ1. 50 orders race for one unit each.
        const int Orders = 50;
        var outcomes = await Task.WhenAll(Enumerable.Range(0, Orders).Select(async index =>
        {
            await using var scope = environment.IsolatedInventory.Services.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IReserveInventoryUseCase>();
            var result = await useCase.ExecuteAsync(
                new ReserveInventoryRequest(Guid.NewGuid(), $"RACE-{index:D3}", [new OrderLineItem("SKU-LIMITED-001", 1)]),
                CancellationToken.None);
            return result.Value.Status;
        }));

        outcomes.Count(status => status == ReservationStatus.Reserved).Should().Be(10);
        outcomes.Count(status => status == ReservationStatus.Failed).Should().Be(Orders - 10);

        await using var db = await environment.OpenAsync(Databases.IsolatedInventory);
        var position = await db.QuerySingleAsync<(int OnHand, int Reserved)>(
            "SELECT sp.on_hand, sp.reserved FROM inventory.stock_positions sp JOIN inventory.products p ON p.id = sp.product_id WHERE p.sku = 'SKU-LIMITED-001'");
        position.Reserved.Should().Be(10);
        position.Reserved.Should().BeLessThanOrEqualTo(position.OnHand);
        (await CountAsync(db, """
            SELECT coalesce(sum(m.quantity), 0) FROM inventory.stock_movements m
            JOIN inventory.stock_positions sp ON sp.id = m.stock_position_id
            JOIN inventory.products p ON p.id = sp.product_id
            WHERE p.sku = 'SKU-LIMITED-001' AND m.movement_type = 'Reservation'
            """)).Should().Be(10, "the ledger records exactly the units that were reserved");
    }

    [Fact]
    public async Task RedeliveredEvent_IsAppliedOnce_ThroughTheInbox()
    {
        var dispatcher = environment.IsolatedInventory.Services.GetRequiredService<IntegrationEventDispatcher>();
        var orderId = Guid.NewGuid();
        var envelope = OrderCreated(orderId, "INBOX-001", "SKU-1004", 4);

        var first = await dispatcher.DispatchAsync(envelope, attempt: 1, CancellationToken.None);
        var second = await dispatcher.DispatchAsync(envelope, attempt: 2, CancellationToken.None);

        first.Should().Be(DispatchOutcome.Handled);
        second.Should().Be(DispatchOutcome.Duplicate);
        await using var db = await environment.OpenAsync(Databases.IsolatedInventory);
        (await CountAsync(db, "SELECT count(*) FROM inventory.reservations WHERE order_id = @orderId", new { orderId })).Should().Be(1);
        (await CountAsync(db, "SELECT count(*) FROM inventory.stock_movements WHERE reference_id = 'INBOX-001'")).Should().Be(1);
        (await CountAsync(db, "SELECT count(*) FROM messaging.outbox_messages WHERE payload->>'orderId' = @id", new { id = orderId.ToString() })).Should().Be(1);
    }

    [Fact]
    public async Task SameOrderPublishedTwiceWithDifferentEventIds_IsReservedOnce()
    {
        // A producer bug or a manual replay can publish the same business fact with a new event id; the inbox
        // cannot catch that, the unique reservation per order does.
        var dispatcher = environment.IsolatedInventory.Services.GetRequiredService<IntegrationEventDispatcher>();
        var orderId = Guid.NewGuid();

        await dispatcher.DispatchAsync(OrderCreated(orderId, "DUP-001", "SKU-1005", 2), 1, CancellationToken.None);
        await dispatcher.DispatchAsync(OrderCreated(orderId, "DUP-001", "SKU-1005", 2), 1, CancellationToken.None);

        await using var db = await environment.OpenAsync(Databases.IsolatedInventory);
        (await CountAsync(db, "SELECT count(*) FROM inventory.stock_movements WHERE reference_id = 'DUP-001'")).Should().Be(1);
    }

    [Fact]
    public async Task Release_ReturnsReservedUnits_AndIsIdempotent()
    {
        await using var scope = environment.IsolatedInventory.Services.CreateAsyncScope();
        var reserve = scope.ServiceProvider.GetRequiredService<IReserveInventoryUseCase>();
        var release = scope.ServiceProvider.GetRequiredService<IReleaseInventoryUseCase>();
        await using var db = await environment.OpenAsync(Databases.IsolatedInventory);
        const string ReservedUnits = "SELECT sum(sp.reserved) FROM inventory.stock_positions sp JOIN inventory.products p ON p.id = sp.product_id WHERE p.sku = 'SKU-1008'";
        var before = await CountAsync(db, ReservedUnits);
        var orderId = Guid.NewGuid();

        (await reserve.ExecuteAsync(new ReserveInventoryRequest(orderId, "REL-001", [new OrderLineItem("SKU-1008", 3)]), CancellationToken.None))
            .Value.Status.Should().Be(ReservationStatus.Reserved);
        (await CountAsync(db, ReservedUnits)).Should().Be(before + 3);

        (await release.ExecuteAsync(new ReleaseInventoryRequest(orderId, "REL-001"), CancellationToken.None)).Value
            .Should().Be(new ReleaseInventoryResponse(ReservationStatus.Released, 3));
        (await release.ExecuteAsync(new ReleaseInventoryRequest(orderId, "REL-001"), CancellationToken.None)).Value.UnitsReleased.Should().Be(0);

        (await CountAsync(db, ReservedUnits)).Should().Be(before);
        (await CountAsync(db, "SELECT count(*) FROM inventory.stock_movements WHERE reference_id = 'REL-001' AND movement_type = 'Release'")).Should().Be(1);
    }

    [Fact]
    public async Task CancellationThatOvertakesTheOrder_LeavesATombstone_SoTheLateOrderReservesNothing()
    {
        await using var scope = environment.IsolatedInventory.Services.CreateAsyncScope();
        var release = scope.ServiceProvider.GetRequiredService<IReleaseInventoryUseCase>();
        var dispatcher = environment.IsolatedInventory.Services.GetRequiredService<IntegrationEventDispatcher>();
        var orderId = Guid.NewGuid();

        await release.ExecuteAsync(new ReleaseInventoryRequest(orderId, "LATE-001"), CancellationToken.None);
        await dispatcher.DispatchAsync(OrderCreated(orderId, "LATE-001", "SKU-1001", 1), 1, CancellationToken.None);

        await using var db = await environment.OpenAsync(Databases.IsolatedInventory);
        (await db.QuerySingleAsync<string>("SELECT status FROM inventory.reservations WHERE order_id = @orderId", new { orderId })).Should().Be("Cancelled");
        (await CountAsync(db, "SELECT count(*) FROM inventory.stock_movements WHERE reference_id = 'LATE-001'")).Should().Be(0);
    }

    private static MessageEnvelope OrderCreated(Guid orderId, string orderNumber, string sku, int quantity)
    {
        var payload = new OrderCreatedEvent(orderId, orderNumber, [new OrderLineItem(sku, quantity)], DateTimeOffset.UtcNow);
        return new MessageEnvelope(
            Guid.CreateVersion7(),
            OrderCreatedEvent.EventType,
            OrderCreatedEvent.Version,
            DateTimeOffset.UtcNow,
            $"test-{orderNumber}",
            null,
            JsonSerializer.SerializeToElement(payload, MessagingJson.Options));
    }
}
