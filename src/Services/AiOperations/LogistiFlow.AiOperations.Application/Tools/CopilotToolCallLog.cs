using LogistiFlow.AiOperations.Domain.Entities;
using LogistiFlow.AiOperations.Domain.Enums;

namespace LogistiFlow.AiOperations.Application.Tools;

/// <summary>The tool calls of one question, in the order they ran. One instance per question.</summary>
public sealed class CopilotToolCallLog
{
    private readonly Lock _gate = new();
    private readonly List<CopilotToolCallEntity> _calls = [];

    public IReadOnlyList<CopilotToolCallEntity> Calls
    {
        get
        {
            lock (_gate)
            {
                return [.. _calls];
            }
        }
    }

    public void Record(string toolName, string argumentsJson, CopilotToolCallOutcome outcome, string summary, long durationMs, DateTimeOffset now)
    {
        lock (_gate)
        {
            _calls.Add(CopilotToolCallEntity.Record(_calls.Count + 1, toolName, argumentsJson, outcome, summary, durationMs, now));
        }
    }
}
