using LogistiFlow.AiOperations.Domain.Enums;
using LogistiFlow.AiOperations.Domain.Models;
using LogistiFlow.BuildingBlocks.Domain.Entities;

namespace LogistiFlow.AiOperations.Domain.Entities;

/// <summary>
/// The audit record of one question: who asked, what was answered, by which provider, which tools ran and what it
/// cost. Written once and never changed. The caller's token is never part of it.
/// </summary>
public sealed class CopilotInteractionEntity : Entity
{
    public const int MaxQuestionLength = 500;

    public const int MaxAnswerLength = 8000;

    private readonly List<CopilotToolCallEntity> _toolCalls;

    private CopilotInteractionEntity(
        Guid conversationId,
        string userEmail,
        string userRole,
        string question,
        string answer,
        CopilotProvider provider,
        CopilotInteractionOutcome outcome,
        string? failureReason,
        CopilotUsage usage,
        long durationMs,
        List<CopilotToolCallEntity> toolCalls)
    {
        ConversationId = conversationId;
        UserEmail = userEmail;
        UserRole = userRole;
        Question = question;
        Answer = answer;
        Provider = provider;
        Outcome = outcome;
        FailureReason = failureReason;
        Usage = usage;
        DurationMs = durationMs;
        _toolCalls = toolCalls;
    }

    public Guid ConversationId { get; }

    public string UserEmail { get; }

    public string UserRole { get; }

    public string Question { get; }

    public string Answer { get; }

    public CopilotProvider Provider { get; }

    public CopilotInteractionOutcome Outcome { get; }

    public string? FailureReason { get; }

    public CopilotUsage Usage { get; }

    public long DurationMs { get; }

    public IReadOnlyList<CopilotToolCallEntity> ToolCalls => _toolCalls;

    public static CopilotInteractionEntity Answered(
        Guid conversationId,
        string userEmail,
        string userRole,
        string question,
        string answer,
        CopilotProvider provider,
        CopilotUsage usage,
        long durationMs,
        IEnumerable<CopilotToolCallEntity> toolCalls,
        DateTimeOffset now) =>
        Create(conversationId, userEmail, userRole, question, answer, provider, CopilotInteractionOutcome.Answered, null, usage, durationMs, toolCalls, now);

    public static CopilotInteractionEntity Failed(
        Guid conversationId,
        string userEmail,
        string userRole,
        string question,
        string failureReason,
        CopilotProvider provider,
        CopilotUsage usage,
        long durationMs,
        IEnumerable<CopilotToolCallEntity> toolCalls,
        DateTimeOffset now) =>
        Create(conversationId, userEmail, userRole, question, string.Empty, provider, CopilotInteractionOutcome.Failed, failureReason, usage, durationMs, toolCalls, now);

    private static CopilotInteractionEntity Create(
        Guid conversationId,
        string userEmail,
        string userRole,
        string question,
        string answer,
        CopilotProvider provider,
        CopilotInteractionOutcome outcome,
        string? failureReason,
        CopilotUsage usage,
        long durationMs,
        IEnumerable<CopilotToolCallEntity> toolCalls,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentNullException.ThrowIfNull(toolCalls);

        var interaction = new CopilotInteractionEntity(
            conversationId == Guid.Empty ? Guid.CreateVersion7(now) : conversationId,
            userEmail,
            userRole,
            Truncate(question.Trim(), MaxQuestionLength),
            Truncate(answer, MaxAnswerLength),
            provider,
            outcome,
            failureReason is null ? null : Truncate(failureReason, 500),
            usage,
            Math.Max(0, durationMs),
            [.. toolCalls.OrderBy(call => call.Sequence)]);
        interaction.Initialize(now);
        return interaction;
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
