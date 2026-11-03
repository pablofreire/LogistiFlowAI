using Dapper;
using LogistiFlow.AiOperations.Domain.Entities;
using LogistiFlow.AiOperations.Domain.Interfaces.Repositories;
using LogistiFlow.AiOperations.Domain.Models;
using LogistiFlow.AiOperations.Infrastructure.Persistence.Queries;
using LogistiFlow.AiOperations.Infrastructure.Persistence.Rows;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;

namespace LogistiFlow.AiOperations.Infrastructure.Persistence.Repositories;

public sealed class CopilotInteractionRepository(IDbSession session) : ICopilotInteractionRepository
{
    public async Task AddAsync(CopilotInteractionEntity interaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(interaction);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            CopilotInteractionQueries.InsertInteraction,
            new
            {
                interaction.PublicId,
                interaction.ConversationId,
                interaction.UserEmail,
                interaction.UserRole,
                interaction.Question,
                interaction.Answer,
                Outcome = interaction.Outcome.ToString(),
                interaction.FailureReason,
                Provider = interaction.Provider.Name,
                interaction.Provider.Model,
                interaction.Provider.Simulated,
                interaction.Usage.InputTokens,
                interaction.Usage.OutputTokens,
                interaction.Usage.TotalTokens,
                interaction.Usage.EstimatedCostUsd,
                interaction.DurationMs,
                interaction.CreatedAt,
                interaction.UpdatedAt,
            },
            session.Transaction,
            cancellationToken: cancellationToken));
        interaction.AssignId(id);

        foreach (var call in interaction.ToolCalls)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                CopilotInteractionQueries.InsertToolCall,
                new
                {
                    call.PublicId,
                    InteractionId = id,
                    call.Sequence,
                    call.ToolName,
                    call.ArgumentsJson,
                    Outcome = call.Outcome.ToString(),
                    call.Summary,
                    call.DurationMs,
                    call.CreatedAt,
                    call.UpdatedAt,
                },
                session.Transaction,
                cancellationToken: cancellationToken));
        }
    }

    public async Task<IReadOnlyList<CopilotTurn>> GetRecentTurnsAsync(Guid conversationId, string userEmail, int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return [];
        }

        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<CopilotTurnRow>(new CommandDefinition(
            CopilotInteractionQueries.RecentTurns,
            new { ConversationId = conversationId, UserEmail = userEmail, Limit = limit },
            session.Transaction,
            cancellationToken: cancellationToken));
        return [.. rows.Reverse().Select(row => new CopilotTurn(row.Question, row.Answer))];
    }
}
