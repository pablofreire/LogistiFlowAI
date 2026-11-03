using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Implementations;

public sealed class ListCopilotInteractionsUseCase(IAiOperationsQueries queries) : IListCopilotInteractionsUseCase
{
    public async Task<Result<PagedResult<CopilotInteractionSummary>>> ExecuteAsync(ListCopilotInteractionsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await queries.ListInteractionsAsync(PageRequest.Normalize(request.Page, request.PageSize), cancellationToken);
    }
}
