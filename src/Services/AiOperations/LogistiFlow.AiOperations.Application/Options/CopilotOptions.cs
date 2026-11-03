namespace LogistiFlow.AiOperations.Application.Options;

/// <summary>Limits that keep each question bounded in time, tool calls and spend.</summary>
public sealed class CopilotOptions
{
    public const string SectionName = "Copilot";

    /// <summary>Model round trips per question; each round may run tools. Stops a model that keeps calling tools.</summary>
    public int MaxToolRounds { get; set; } = 6;

    public int MaxOutputTokens { get; set; } = 800;

    /// <summary>Whole-question budget, tool calls included.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(45);

    /// <summary>Earlier answered turns of the conversation replayed as context.</summary>
    public int HistoryTurns { get; set; } = 5;

    /// <summary>Shipments or exceptions read by one list tool (one page of the service).</summary>
    public int ScanLimit { get; set; } = 100;

    /// <summary>Price used for the cost estimate; zero when unknown.</summary>
    public decimal InputCostPerThousandTokensUsd { get; set; }

    public decimal OutputCostPerThousandTokensUsd { get; set; }
}
