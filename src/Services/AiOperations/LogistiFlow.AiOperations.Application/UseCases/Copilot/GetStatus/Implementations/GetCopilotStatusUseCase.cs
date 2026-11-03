using LogistiFlow.AiOperations.Application.Agent;
using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Tools;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Implementations;

/// <summary>Tells the console which provider answers, so a simulated provider is labelled before the first question.</summary>
public sealed class GetCopilotStatusUseCase(ICopilotChatClientFactory chatClients) : IGetCopilotStatusUseCase
{
    public Task<Result<GetCopilotStatusResponse>> ExecuteAsync(GetCopilotStatusRequest request, CancellationToken cancellationToken)
    {
        var provider = chatClients.Provider;
        Result<GetCopilotStatusResponse> result = new GetCopilotStatusResponse(
            provider.Name,
            provider.Model,
            provider.Simulated,
            OperationsToolNames.All,
            CopilotPrompts.SampleQuestions);
        return Task.FromResult(result);
    }
}
