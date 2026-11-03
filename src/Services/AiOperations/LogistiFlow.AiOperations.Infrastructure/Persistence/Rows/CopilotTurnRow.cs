namespace LogistiFlow.AiOperations.Infrastructure.Persistence.Rows;

internal sealed class CopilotTurnRow
{
    public string Question { get; init; } = string.Empty;

    public string Answer { get; init; } = string.Empty;
}
