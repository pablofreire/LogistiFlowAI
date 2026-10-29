using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Implementations;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Validators;
using LogistiFlow.Fulfillment.Domain.Entities;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;
using NSubstitute;

namespace LogistiFlow.Fulfillment.UnitTests.Application.UseCases.Jobs.PerformAction.Implementations;

public sealed class PerformFulfillmentJobActionUseCaseTests
{
    private readonly IFulfillmentJobRepository _jobs = Substitute.For<IFulfillmentJobRepository>();
    private readonly IIntegrationEventOutbox _outbox = Substitute.For<IIntegrationEventOutbox>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PerformFulfillmentJobActionUseCase _useCase;

    public PerformFulfillmentJobActionUseCaseTests()
    {
        _unitOfWork.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<PerformFulfillmentJobActionResponse>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<Result<PerformFulfillmentJobActionResponse>>>>()(CancellationToken.None));
        _jobs.TryUpdateStatusAsync(Arg.Any<FulfillmentJobEntity>(), Arg.Any<CancellationToken>()).Returns(true);
        _useCase = new PerformFulfillmentJobActionUseCase(new PerformFulfillmentJobActionRequestValidator(), _jobs, _outbox, _unitOfWork, TimeProvider.System);
    }

    [Fact]
    public async Task Pack_PublishesPackedOnly()
    {
        var job = Job(FulfillmentJobStatus.Picking);

        var result = await _useCase.ExecuteAsync(new(job.PublicId, FulfillmentJobAction.Pack, null, "ana@example.com"), CancellationToken.None);

        result.Value.Status.Should().Be(FulfillmentJobStatus.Packed);
        await _outbox.Received(1).EnqueueAsync(Arg.Is<FulfillmentPackedEvent>(e => e.OrderId == job.OrderId), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync<FulfillmentShippedEvent>(default!, default);
    }

    [Fact]
    public async Task Ship_PublishesShippedWithTheLinesAndTheReservation()
    {
        var job = Job(FulfillmentJobStatus.Packed);

        var result = await _useCase.ExecuteAsync(new(job.PublicId, FulfillmentJobAction.Ship, null, "ana@example.com"), CancellationToken.None);

        result.Value.Status.Should().Be(FulfillmentJobStatus.Shipped);
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<FulfillmentShippedEvent>(e => e.ReservationId == job.ReservationId && e.Lines.Count == 1 && e.Lines[0].Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportIssue_PublishesNothing()
    {
        var job = Job(FulfillmentJobStatus.Picking);

        var result = await _useCase.ExecuteAsync(new(job.PublicId, FulfillmentJobAction.ReportIssue, "Damaged carton", "ana@example.com"), CancellationToken.None);

        result.Value.Status.Should().Be(FulfillmentJobStatus.Failed);
        _outbox.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ReportIssue_WithoutReason_IsAValidationError()
    {
        var result = await _useCase.ExecuteAsync(new(Guid.NewGuid(), FulfillmentJobAction.ReportIssue, " ", "ana@example.com"), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Validation);
        await _jobs.DidNotReceiveWithAnyArgs().GetByPublicIdAsync(default, default);
    }

    [Fact]
    public async Task UnknownJob_IsNotFound()
    {
        var result = await _useCase.ExecuteAsync(new(Guid.NewGuid(), FulfillmentJobAction.Pack, null, "ana@example.com"), CancellationToken.None);

        result.Error.Code.Should().Be("fulfillment.job_not_found");
    }

    [Fact]
    public async Task ConcurrentChange_IsAConflictAndPublishesNothing()
    {
        var job = Job(FulfillmentJobStatus.Picking);
        _jobs.TryUpdateStatusAsync(job, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _useCase.ExecuteAsync(new(job.PublicId, FulfillmentJobAction.Pack, null, "ana@example.com"), CancellationToken.None);

        result.Error.Code.Should().Be("fulfillment.concurrent_modification");
        _outbox.ReceivedCalls().Should().BeEmpty();
    }

    private FulfillmentJobEntity Job(FulfillmentJobStatus status)
    {
        var job = FulfillmentJobEntity.Restore(
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ORD-1001",
            "WH-NJ1",
            Guid.NewGuid(),
            status,
            null,
            2,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            [new FulfillmentJobLineEntity("SKU-1001", 2)]);
        _jobs.GetByPublicIdAsync(job.PublicId, Arg.Any<CancellationToken>()).Returns(job);
        return job;
    }
}
