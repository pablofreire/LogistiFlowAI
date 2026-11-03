using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.AiOperations.Domain.Errors;

public static class AiOperationsErrors
{
    public static readonly Error ProviderUnavailable = Error.Unavailable(
        "ai.provider_unavailable",
        "The AI provider did not answer. The logistics platform is unaffected; try again later.");

    public static readonly Error QuestionTimedOut = Error.Unavailable(
        "ai.question_timed_out",
        "The copilot took too long to answer. Try a narrower question.");
}
