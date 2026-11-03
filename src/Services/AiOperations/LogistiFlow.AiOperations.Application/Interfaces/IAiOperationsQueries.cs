using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Contracts;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;

namespace LogistiFlow.AiOperations.Application.Interfaces;

/// <summary>Read side of the audit trail.</summary>
public interface IAiOperationsQueries
{
    Task<PagedResult<CopilotInteractionSummary>> ListInteractionsAsync(PageRequest page, CancellationToken cancellationToken);
}
