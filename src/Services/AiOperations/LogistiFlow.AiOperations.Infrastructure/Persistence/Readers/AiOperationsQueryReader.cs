using Dapper;
using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Contracts;
using LogistiFlow.AiOperations.Infrastructure.Persistence.Queries;
using LogistiFlow.AiOperations.Infrastructure.Persistence.Rows;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;

namespace LogistiFlow.AiOperations.Infrastructure.Persistence.Readers;

/// <summary>Read side of the audit trail: projections straight from SQL into API models.</summary>
public sealed class AiOperationsQueryReader(IDbSession session) : IAiOperationsQueries
{
    public async Task<PagedResult<CopilotInteractionSummary>> ListInteractionsAsync(PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(CopilotInteractionQueries.CountInteractions, transaction: session.Transaction, cancellationToken: cancellationToken));
        var rows = await connection.QueryAsync<CopilotInteractionSummaryRow>(new CommandDefinition(
            CopilotInteractionQueries.ListInteractions,
            new { Limit = page.PageSize, Offset = (page.Page - 1) * page.PageSize },
            session.Transaction,
            cancellationToken: cancellationToken));

        var items = rows.Select(row => new CopilotInteractionSummary(
                row.Id,
                row.ConversationId,
                row.UserEmail,
                row.UserRole,
                row.Question,
                row.Outcome,
                row.FailureReason,
                row.Provider,
                row.Model,
                row.Simulated,
                row.Tools.Split(',', StringSplitOptions.RemoveEmptyEntries),
                row.TotalTokens,
                row.EstimatedCostUsd,
                row.DurationMs,
                row.CreatedAt))
            .ToList();
        return new PagedResult<CopilotInteractionSummary>(items, page.Page, page.PageSize, total);
    }
}
