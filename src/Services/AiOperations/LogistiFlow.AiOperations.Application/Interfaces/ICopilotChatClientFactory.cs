using LogistiFlow.AiOperations.Domain.Models;
using Microsoft.Extensions.AI;

namespace LogistiFlow.AiOperations.Application.Interfaces;

/// <summary>
/// The model behind the copilot: an OpenAI-compatible endpoint when one is configured, otherwise the deterministic
/// demo provider. The caller owns the returned client.
/// </summary>
public interface ICopilotChatClientFactory
{
    CopilotProvider Provider { get; }

    IChatClient Create();
}
