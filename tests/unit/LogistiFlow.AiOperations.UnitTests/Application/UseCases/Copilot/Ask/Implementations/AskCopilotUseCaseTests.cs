using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Application.Options;
using LogistiFlow.AiOperations.Application.Tools;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Implementations;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Validators;
using LogistiFlow.AiOperations.Domain.Entities;
using LogistiFlow.AiOperations.Domain.Enums;
using LogistiFlow.AiOperations.Domain.Interfaces.Repositories;
using LogistiFlow.AiOperations.Domain.Models;
using LogistiFlow.AiOperations.Infrastructure.Options;
using LogistiFlow.AiOperations.Infrastructure.Providers;
using LogistiFlow.AiOperations.Infrastructure.Providers.Demo;
using LogistiFlow.AiOperations.UnitTests.Fixtures;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LogistiFlow.AiOperations.UnitTests.Application.UseCases.Copilot.Ask.Implementations;

/// <summary>
/// The whole agent path in memory: the deterministic demo provider, the real Agent Framework tool loop and the real
/// tools over faked services. What the answer says must come from what the fakes returned.
/// </summary>
public sealed class AskCopilotUseCaseTests
{
    private readonly IOrdersApi _orders = Substitute.For<IOrdersApi>();
    private readonly IInventoryApi _inventory = Substitute.For<IInventoryApi>();
    private readonly IFulfillmentApi _fulfillment = Substitute.For<IFulfillmentApi>();
    private readonly IShippingApi _shipping = Substitute.For<IShippingApi>();
    private readonly ICopilotInteractionRepository _interactions = Substitute.For<ICopilotInteractionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CopilotOptions _options = new() { Timeout = TimeSpan.FromSeconds(10) };
    private CopilotInteractionEntity? _saved;

    public AskCopilotUseCaseTests()
    {
        _unitOfWork.ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _interactions.AddAsync(Arg.Do<CopilotInteractionEntity>(interaction => _saved = interaction), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _interactions.GetRecentTurnsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _orders.FindByNumberAsync("ORD-1025", Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(OperationsData.Order()));
        _orders.GetHistoryAsync(OperationsData.OrderId, Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(OperationsData.History()));
    }

    [Fact]
    public async Task DelayQuestion_RunsTheDelayAnalysis_AndAnswersFromItsResult()
    {
        _shipping.GetShipmentByOrderAsync(OperationsData.OrderId, Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Ok(OperationsData.Shipment(exceptions: [OperationsData.Delay()])));

        var result = await UseCase().ExecuteAsync(Ask("Why is order ORD-1025 delayed?"), CancellationToken.None);

        var response = result.Value;
        response.Simulated.Should().BeTrue();
        response.Provider.Should().Be("demo");
        response.ToolCalls.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Sequence = 1,
            Name = OperationsToolNames.AnalyzeShipmentDelay,
            Arguments = """{"orderNumber":"ORD-1025"}""",
            Outcome = "ok",
        });
        response.Answer.Should().Contain("ORD-1025: Delayed: the carrier reported a delay and the estimate is 2 day(s) after the promised date.")
            .And.Contain("severe weather at the sort facility")
            .And.Contain("Promised delivery: 2026-11-06. Carrier estimate: 2026-11-08")
            .And.EndWith(DemoAnswerComposer.SimulatedFooter);
        response.Usage.EstimatedCostUsd.Should().Be(0m);
        response.Usage.TotalTokens.Should().BeNull();

        _saved!.Outcome.Should().Be(CopilotInteractionOutcome.Answered);
        _saved.UserEmail.Should().Be("ops.manager@logistiflow.local");
        _saved.Provider.Should().Be(new CopilotProvider("demo", "deterministic-demo-v1", true));
        _saved.ToolCalls.Should().ContainSingle().Which.Outcome.Should().Be(CopilotToolCallOutcome.Ok);
    }

    [Fact]
    public async Task ToolThatFindsNothing_IsReportedAsSuch_NeverFilledIn()
    {
        _orders.FindByNumberAsync("ORD-9999", Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Fail<OrderView>(CopilotToolCallOutcome.NotFound, "Order ORD-9999 does not exist."));

        var result = await UseCase().ExecuteAsync(Ask("Why is order ORD-9999 delayed?"), CancellationToken.None);

        result.Value.Answer.Should().StartWith("I could not analyse the shipment: Order ORD-9999 does not exist. (status: not_found). I will not guess the missing data.");
        result.Value.ToolCalls.Should().ContainSingle().Which.Outcome.Should().Be("not_found");
        await _shipping.DidNotReceiveWithAnyArgs().GetShipmentByOrderAsync(default, default);
    }

    [Fact]
    public async Task ServiceForbidsTheUser_TheAnswerSaysSo()
    {
        _orders.FindByNumberAsync("ORD-1025", Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Fail<OrderView>(CopilotToolCallOutcome.Forbidden, "Orders does not allow this user to read it."));

        var result = await UseCase().ExecuteAsync(Ask("Why is order ORD-1025 delayed?"), CancellationToken.None);

        result.Value.Answer.Should().Contain("Orders does not allow this user to read it. (status: forbidden)");
        _saved!.ToolCalls.Single().Outcome.Should().Be(CopilotToolCallOutcome.Forbidden);
    }

    [Fact]
    public async Task ReservationQuestion_ReadsTheOrderThenTheStockOfItsSkus_InTwoRounds()
    {
        _orders.FindByNumberAsync("ORD-1025", Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Ok(OperationsData.Order("Failed", "Insufficient inventory: SKU-OOS-001 requested 1, available 0", new OrderLineView(1, "SKU-OOS-001", 1))));
        _inventory.GetAvailabilityAsync("SKU-OOS-001", Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Ok(new SkuAvailabilityView("SKU-OOS-001", "Discontinued widget", 0, [new WarehouseStockView("WH-CA1", 0, 0, 0)])));

        var result = await UseCase().ExecuteAsync(Ask("What caused the inventory reservation failure for ORD-1025?"), CancellationToken.None);

        result.Value.ToolCalls.Select(call => call.Name).Should().Equal(OperationsToolNames.GetOrderDetails, OperationsToolNames.GetInventoryAvailability);
        result.Value.Answer.Should().Contain("ORD-1025 failed: Insufficient inventory: SKU-OOS-001 requested 1, available 0.")
            .And.Contain("Stock of SKU-OOS-001 now: 0 available (WH-CA1 0).");
    }

    [Fact]
    public async Task FollowUpWithoutAnOrderNumber_TakesItFromTheConversation()
    {
        var conversation = Guid.CreateVersion7();
        _interactions.GetRecentTurnsAsync(conversation, "ops.manager@logistiflow.local", 5, Arg.Any<CancellationToken>())
            .Returns([new CopilotTurn("What is the status of ORD-1025?", "ORD-1025 is Shipped.")]);
        _shipping.GetShipmentByOrderAsync(OperationsData.OrderId, Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Ok(OperationsData.Shipment(exceptions: [OperationsData.Delay()])));

        var result = await UseCase().ExecuteAsync(Ask("And why is it late?", conversation), CancellationToken.None);

        result.Value.ConversationId.Should().Be(conversation);
        result.Value.ToolCalls.Should().ContainSingle().Which.Arguments.Should().Be("""{"orderNumber":"ORD-1025"}""");
    }

    [Fact]
    public async Task UnrecognisedQuestion_ListsTheSupportedOnes_WithoutCallingTools()
    {
        var result = await UseCase().ExecuteAsync(Ask("Tell me a joke"), CancellationToken.None);

        result.Value.ToolCalls.Should().BeEmpty();
        result.Value.Answer.Should().Contain("Why is order ORD-1025 delayed?").And.Contain("demo mode");
    }

    [Fact]
    public async Task ProviderFailure_Is503_AndStillAudited()
    {
        var failing = Substitute.For<IChatClient>();
        failing.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("model endpoint down"));
        var factory = Substitute.For<ICopilotChatClientFactory>();
        factory.Provider.Returns(new CopilotProvider("openai", "gpt-test", false));
        factory.Create().Returns(failing);

        var result = await UseCase(factory).ExecuteAsync(Ask("Why is order ORD-1025 delayed?"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unavailable);
        _saved!.Outcome.Should().Be(CopilotInteractionOutcome.Failed);
        _saved.Provider.Simulated.Should().BeFalse();
    }

    [Fact]
    public async Task TooLongQuestion_IsRejected_BeforeAnyModelCall()
    {
        var result = await UseCase().ExecuteAsync(Ask(new string('x', 501)), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Validation);
        await _interactions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task UsageAndCost_ComeFromTheProvider()
    {
        var client = Substitute.For<IChatClient>();
        client.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ORD-1025 is in transit."))
            {
                Usage = new UsageDetails { InputTokenCount = 1200, OutputTokenCount = 300, TotalTokenCount = 1500 },
            });
        var factory = Substitute.For<ICopilotChatClientFactory>();
        factory.Provider.Returns(new CopilotProvider("openai", "gpt-test", false));
        factory.Create().Returns(client);
        _options.InputCostPerThousandTokensUsd = 0.5m;
        _options.OutputCostPerThousandTokensUsd = 1.5m;

        var result = await UseCase(factory).ExecuteAsync(Ask("Where is ORD-1025?"), CancellationToken.None);

        result.Value.Simulated.Should().BeFalse();
        result.Value.Usage.Should().Be(new CopilotUsageItem(1200, 300, 1500, 1.05m));
    }

    private static AskCopilotRequest Ask(string question, Guid? conversationId = null) =>
        new(question, conversationId, "ops.manager@logistiflow.local", "OpsManager");

    private AskCopilotUseCase UseCase(ICopilotChatClientFactory? factory = null)
    {
        var options = Microsoft.Extensions.Options.Options.Create(_options);
        var log = new CopilotToolCallLog();
        var toolset = new OperationsToolset(_orders, _inventory, _fulfillment, _shipping, log, options, TimeProvider.System);
        return new AskCopilotUseCase(
            new AskCopilotRequestValidator(),
            factory ?? new CopilotChatClientFactory(Microsoft.Extensions.Options.Options.Create(new AiProviderOptions())),
            toolset,
            log,
            _interactions,
            _unitOfWork,
            options,
            TimeProvider.System,
            NullLogger<AskCopilotUseCase>.Instance);
    }
}
