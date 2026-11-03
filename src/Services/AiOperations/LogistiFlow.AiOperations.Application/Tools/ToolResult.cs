using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Domain.Enums;

namespace LogistiFlow.AiOperations.Application.Tools;

/// <summary>Builds <see cref="ToolResult{T}"/> values and names the statuses the model sees.</summary>
public static class ToolResult
{
    public const string OkStatus = "ok";

    public static ToolResult<T> Ok<T>(T data) => new(OkStatus, null, data);

    public static ToolResult<T> Fail<T>(CopilotToolCallOutcome outcome, string message) => new(StatusOf(outcome), message, default);

    /// <summary>Passes a failed service call on to the model, with its reason.</summary>
    public static ToolResult<T> From<T>(IServiceReply reply)
    {
        ArgumentNullException.ThrowIfNull(reply);
        return Fail<T>(reply.Outcome, reply.Message ?? "The service returned no data.");
    }

    public static string StatusOf(CopilotToolCallOutcome outcome) => outcome switch
    {
        CopilotToolCallOutcome.Ok => OkStatus,
        CopilotToolCallOutcome.NotFound => "not_found",
        CopilotToolCallOutcome.Forbidden => "forbidden",
        CopilotToolCallOutcome.Invalid => "invalid",
        _ => "unavailable",
    };

    public static CopilotToolCallOutcome OutcomeOf(string status) => status switch
    {
        OkStatus => CopilotToolCallOutcome.Ok,
        "not_found" => CopilotToolCallOutcome.NotFound,
        "forbidden" => CopilotToolCallOutcome.Forbidden,
        "invalid" => CopilotToolCallOutcome.Invalid,
        _ => CopilotToolCallOutcome.Unavailable,
    };
}

/// <summary>
/// What every tool returns to the model: <c>Status</c> "ok" with <c>Data</c>, or another status with a
/// <c>Message</c> and no data. The instructions tell the model to report a non-ok status rather than guess.
/// </summary>
public sealed record ToolResult<T>(string Status, string? Message, T? Data);
