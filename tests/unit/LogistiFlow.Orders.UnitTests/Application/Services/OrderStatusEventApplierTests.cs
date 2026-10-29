using LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Entities;
using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LogistiFlow.Orders.UnitTests.Application.Services;

public sealed class OrderStatusEventApplierTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly OrderStatusEventApplier _applier;

    public OrderStatusEventApplierTests()
    {
        _applier = new OrderStatusEventApplier(_repository, TimeProvider.System, NullLogger<OrderStatusEventApplier>.Instance);
    }

    [Fact]
    public async Task ApplyAsync_ValidTransition_PersistsIt()
    {
        var order = Order(OrderStatus.PendingInventory);
        _repository.TryUpdateStatusAsync(order, Arg.Any<CancellationToken>()).Returns(true);

        await _applier.ApplyAsync(order.PublicId, OrderStatus.Reserved, "Reserved", "test", CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Reserved);
        await _repository.Received(1).TryUpdateStatusAsync(order, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_StaleEvent_IsIgnoredWithoutWriting()
    {
        var order = Order(OrderStatus.Cancelled);

        await _applier.ApplyAsync(order.PublicId, OrderStatus.Reserved, "Reserved", "test", CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        await _repository.DidNotReceiveWithAnyArgs().TryUpdateStatusAsync(default!, default);
    }

    [Fact]
    public async Task ApplyAsync_EventBeforeItsPrerequisite_ThrowsTransientSoItIsRetried()
    {
        var order = Order(OrderStatus.PendingInventory);

        var act = () => _applier.ApplyAsync(order.PublicId, OrderStatus.Picking, "Started", "test", CancellationToken.None);

        await act.Should().ThrowAsync<TransientIntegrationEventException>();
    }

    [Fact]
    public async Task ApplyAsync_LostOptimisticConcurrencyRace_ThrowsTransient()
    {
        var order = Order(OrderStatus.PendingInventory);
        _repository.TryUpdateStatusAsync(order, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _applier.ApplyAsync(order.PublicId, OrderStatus.Reserved, "Reserved", "test", CancellationToken.None);

        await act.Should().ThrowAsync<TransientIntegrationEventException>();
    }

    private OrderEntity Order(OrderStatus status)
    {
        var order = OrderEntity.Restore(
            1, Guid.CreateVersion7(), "ORD-1001", "Acme", "Austin", "US", null, status, null, 1, "key", "hash", true,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, []);
        _repository.GetByPublicIdAsync(order.PublicId, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }
}
