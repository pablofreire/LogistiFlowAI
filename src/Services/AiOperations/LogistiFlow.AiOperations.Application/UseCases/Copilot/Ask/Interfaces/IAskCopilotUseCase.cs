using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;

namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Interfaces;

public interface IAskCopilotUseCase : IUseCase<AskCopilotRequest, AskCopilotResponse>;
