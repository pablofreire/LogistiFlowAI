using LogistiFlow.AiOperations.Domain.Entities;
using LogistiFlow.AiOperations.Domain.Models;

namespace LogistiFlow.AiOperations.Domain.Interfaces.Repositories;

public interface ICopilotInteractionRepository
{
    /// <summary>Writes the interaction and its tool calls.</summary>
    Task AddAsync(CopilotInteractionEntity interaction, CancellationToken cancellationToken);

    /// <summary>
    /// The last <paramref name="limit"/> answered turns of a conversation, oldest first. Only the asking user's own
    /// turns are returned, so a conversation id cannot be used to read someone else's questions.
    /// </summary>
    Task<IReadOnlyList<CopilotTurn>> GetRecentTurnsAsync(Guid conversationId, string userEmail, int limit, CancellationToken cancellationToken);
}
