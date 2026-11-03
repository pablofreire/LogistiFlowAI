using LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Contracts;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;

namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Interfaces;

public interface IGetCopilotStatusUseCase : IUseCase<GetCopilotStatusRequest, GetCopilotStatusResponse>;
