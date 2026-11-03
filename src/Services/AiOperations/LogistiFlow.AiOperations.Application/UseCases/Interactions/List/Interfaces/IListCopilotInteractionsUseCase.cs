using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Contracts;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;

namespace LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Interfaces;

public interface IListCopilotInteractionsUseCase : IUseCase<ListCopilotInteractionsRequest, PagedResult<CopilotInteractionSummary>>;
