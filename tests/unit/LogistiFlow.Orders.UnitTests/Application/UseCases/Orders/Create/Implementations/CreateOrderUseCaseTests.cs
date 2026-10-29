using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Implementations;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Validators;
using LogistiFlow.Orders.Domain.Entities;
using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;
using LogistiFlow.Orders.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LogistiFlow.Orders.UnitTests.Application.UseCases.Orders.Create.Implementations;

public sealed class CreateOrderUseCaseTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IIntegrationEventOutbox _outbox = Substitute.For<IIntegrationEventOutbox>();
    private readonly PassThroughUnitOfWork _unitOfWork = new();
    private readonly CreateOrderUseCase _useCase;

    public CreateOrderUseCaseTests()
    {
        _useCase = new CreateOrderUseCase(
            new CreateOrderRequestValidator(),
            _repository,
            _outbox,
            _unitOfWork,
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero)));

        _repository.TryAddAsync(Arg.Any<OrderEntity>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var order = callInfo.Arg<OrderEntity>();
            order.AssignId(42);
            order.AssignOrderNumber("ORD-1042");
            return true;
        });
    }

    [Fact]
    public async Task ExecuteAsync_NewKey_StoresOrderAndEnqueuesOrderCreated()
    {
        var result = await _useCase.ExecuteAsync(Request(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Replayed.Should().BeFalse();
        result.Value.Order.OrderNumber.Should().Be("ORD-1042");
        result.Value.Order.Status.Should().Be(OrderStatus.PendingInventory);
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<OrderCreatedEvent>(e => e.OrderNumber == "ORD-1042" && e.Lines.Count == 1 && e.Lines[0].Sku == "SKU-1001"),
            Arg.Any<CancellationToken>());
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_KnownKeySameBody_ReplaysWithoutWriting()
    {
        var request = Request();
        _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(StoredOrder(RequestHasher.Hash(request)));

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Replayed.Should().BeTrue();
        await _repository.DidNotReceive().TryAddAsync(Arg.Any<OrderEntity>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync<OrderCreatedEvent>(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_KnownKeyDifferentBody_ReturnsConflict()
    {
        var request = Request();
        _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(StoredOrder("a-different-hash"));

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("order.idempotency_key_reused");
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentDuplicateWinsTheInsert_ReplaysTheWinner()
    {
        var request = Request();
        _repository.TryAddAsync(Arg.Any<OrderEntity>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(null, StoredOrder(RequestHasher.Hash(request)));

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Replayed.Should().BeTrue();
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync<OrderCreatedEvent>(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidRequest_ReturnsValidationErrorWithoutTouchingStorage()
    {
        var result = await _useCase.ExecuteAsync(Request() with { Lines = [] }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("request.invalid");
        await _repository.DidNotReceiveWithAnyArgs().GetByIdempotencyKeyAsync(default!, default);
    }

    private static CreateOrderRequest Request() =>
        new("key-001", "Acme Retail", "Austin", "US", null, [new CreateOrderLineRequest("sku-1001", 2)]);

    private static OrderEntity StoredOrder(string requestHash) =>
        OrderEntity.Restore(
            7,
            Guid.CreateVersion7(),
            "ORD-1007",
            "Acme Retail",
            "Austin",
            "US",
            null,
            OrderStatus.Reserved,
            null,
            2,
            "key-001",
            requestHash,
            true,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            [new OrderLineEntity(1, "SKU-1001", 2)]);
}
