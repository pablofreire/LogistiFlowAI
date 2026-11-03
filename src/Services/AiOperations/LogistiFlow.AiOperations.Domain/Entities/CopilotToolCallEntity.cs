using LogistiFlow.AiOperations.Domain.Enums;
using LogistiFlow.BuildingBlocks.Domain.Entities;

namespace LogistiFlow.AiOperations.Domain.Entities;

/// <summary>One tool the agent ran while answering, in the order it ran. Arguments are stored, results are not.</summary>
public sealed class CopilotToolCallEntity : Entity
{
    public const int MaxArgumentsLength = 1000;

    public const int MaxSummaryLength = 300;

    private CopilotToolCallEntity(int sequence, string toolName, string argumentsJson, CopilotToolCallOutcome outcome, string summary, long durationMs)
    {
        Sequence = sequence;
        ToolName = toolName;
        ArgumentsJson = argumentsJson;
        Outcome = outcome;
        Summary = summary;
        DurationMs = durationMs;
    }

    public int Sequence { get; }

    public string ToolName { get; }

    public string ArgumentsJson { get; }

    public CopilotToolCallOutcome Outcome { get; }

    /// <summary>A one-line description of what came back, for example "3 exceptions" or "order not found".</summary>
    public string Summary { get; }

    public long DurationMs { get; }

    public static CopilotToolCallEntity Record(
        int sequence,
        string toolName,
        string argumentsJson,
        CopilotToolCallOutcome outcome,
        string summary,
        long durationMs,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var call = new CopilotToolCallEntity(
            sequence,
            toolName,
            Truncate(argumentsJson, MaxArgumentsLength),
            outcome,
            Truncate(summary, MaxSummaryLength),
            Math.Max(0, durationMs));
        call.Initialize(now);
        return call;
    }

    private static string Truncate(string? value, int length) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= length ? value : value[..length];
}
