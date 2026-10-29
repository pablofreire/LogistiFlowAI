using LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Entities;
using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LogistiFlow.Orders.UnitTests.Application.Services;

/// <summary>Phase 2 events: packing, shipping, delivery and returns, including out-of-order arrival.</summary>
public sealed class OrderStatusEventApplierShippingTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly OrderStatusEventApplier _applier;

    public OrderStatusEventApplierShippingTests()
    {
        _repository.TryUpdateStatusAsync(Arg.Any<OrderEntity>(), Arg.Any<CancellationToken>()).Returns(true);
        _applier = new OrderStatusEventApplier(_repository, TimeProvider.System, NullLogger<OrderStatusEventApplier>.Instance);
    }

    [Theory]
    [InlineData(OrderStatus.Picking, OrderStatus.Packed)]
    [InlineData(OrderStatus.Packed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public async Task HappyPathEvent_IsApplied(OrderStatus current, OrderStatus target)
    {
        var order = Order(current);

        await _applier.ApplyAsync(order.PublicId, target, "reason", "test", CancellationToken.None);

        order.Status.Should().Be(target);
    }

    [Theory]
    [InlineData(OrderStatus.Picking, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Packed, OrderStatus.Delivered)]
    public async Task EventThatOvertookItsPredecessor_IsRetried(OrderStatus current, OrderStatus target)
    {
        var order = Order(current);

        var act = () => _applier.ApplyAsync(order.PublicId, target, "reason", "test", CancellationToken.None);

        await act.Should().ThrowAsync<TransientIntegrationEventException>();
        order.Status.Should().Be(current);
    }

    [Fact]
    public async Task Return_OfAShippedOrder_FailsIt()
    {
        var order = Order(OrderStatus.Shipped);

        await _applier.ApplyAsync(order.PublicId, OrderStatus.Failed, "Address not located", "test", CancellationToken.None, OrderStatus.Shipped);

        order.Status.Should().Be(OrderStatus.Failed);
    }

    [Theory]
    [InlineData(OrderStatus.Picking)]
    [InlineData(OrderStatus.Packed)]
    public async Task Return_BeforeTheOrderIsShipped_IsRetriedInsteadOfSkippingSteps(OrderStatus current)
    {
        var order = Order(current);

        var act = () => _applier.ApplyAsync(order.PublicId, OrderStatus.Failed, "Address not located", "test", CancellationToken.None, OrderStatus.Shipped);

        await act.Should().ThrowAsync<TransientIntegrationEventException>();
        order.Status.Should().Be(current);
    }

    [Fact]
    public async Task Return_AfterDelivery_IsStale()
    {
        var order = Order(OrderStatus.Delivered);

        await _applier.ApplyAsync(order.PublicId, OrderStatus.Failed, "Address not located", "test", CancellationToken.None, OrderStatus.Shipped);

        order.Status.Should().Be(OrderStatus.Delivered);
        await _repository.DidNotReceiveWithAnyArgs().TryUpdateStatusAsync(default!, default);
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
