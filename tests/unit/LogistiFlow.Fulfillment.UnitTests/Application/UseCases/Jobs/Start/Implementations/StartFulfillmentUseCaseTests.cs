using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Implementations;
using LogistiFlow.Fulfillment.Domain.Entities;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;
using NSubstitute;

namespace LogistiFlow.Fulfillment.UnitTests.Application.UseCases.Jobs.Start.Implementations;

public sealed class StartFulfillmentUseCaseTests
{
    private readonly IFulfillmentJobRepository _jobs = Substitute.For<IFulfillmentJobRepository>();
    private readonly IIntegrationEventOutbox _outbox = Substitute.For<IIntegrationEventOutbox>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly StartFulfillmentUseCase _useCase;

    public StartFulfillmentUseCaseTests()
    {
        _unitOfWork.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<StartFulfillmentResponse>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<Result<StartFulfillmentResponse>>>>()(CancellationToken.None));
        _useCase = new StartFulfillmentUseCase(_jobs, _outbox, _unitOfWork, TimeProvider.System);
    }

    [Fact]
    public async Task ExecuteAsync_NewOrder_CreatesJobInPickingAndPublishesStarted()
    {
        _jobs.TryAddAsync(Arg.Any<FulfillmentJobEntity>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _useCase.ExecuteAsync(Request(), CancellationToken.None);

        result.Value.Status.Should().Be(FulfillmentJobStatus.Picking);
        result.Value.AlreadyExisted.Should().BeFalse();
        await _outbox.Received(1).EnqueueAsync(Arg.Is<FulfillmentStartedEvent>(e => e.WarehouseCode == "WH-NJ1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_JobAlreadyExists_ReturnsItWithoutPublishing()
    {
        var request = Request();
        var existing = FulfillmentJobEntity.CreateAndStart(request.OrderId, request.OrderNumber, "WH-NJ1", request.ReservationId, [], "test", DateTimeOffset.UnixEpoch);
        _jobs.GetByOrderIdAsync(request.OrderId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.Value.AlreadyExisted.Should().BeTrue();
        result.Value.JobId.Should().Be(existing.PublicId);
        await _jobs.DidNotReceiveWithAnyArgs().TryAddAsync(default!, default);
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync<FulfillmentStartedEvent>(default!, default);
    }

    private static StartFulfillmentRequest Request() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "ORD-1001", "WH-NJ1", [new OrderLineItem("SKU-1001", 2)], "inventory.inventory-reserved");
}
