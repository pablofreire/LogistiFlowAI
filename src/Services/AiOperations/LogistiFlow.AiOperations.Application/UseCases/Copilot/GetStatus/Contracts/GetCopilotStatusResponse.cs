namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Contracts;

/// <summary>Which provider answers and the questions the demo provider recognises.</summary>
public sealed record GetCopilotStatusResponse(
    string Provider,
    string Model,
    bool Simulated,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> SampleQuestions);
