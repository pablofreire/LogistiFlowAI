namespace LogistiFlow.AiOperations.Infrastructure.Persistence.Queries;

internal static class CopilotInteractionQueries
{
    public const string InsertInteraction = """
        INSERT INTO aiops.copilot_interactions (
            public_id, conversation_id, user_email, user_role, question, answer, outcome, failure_reason,
            provider, model, simulated, input_tokens, output_tokens, total_tokens, estimated_cost_usd, duration_ms,
            is_active, created_at, updated_at)
        VALUES (
            @PublicId, @ConversationId, @UserEmail, @UserRole, @Question, @Answer, @Outcome, @FailureReason,
            @Provider, @Model, @Simulated, @InputTokens, @OutputTokens, @TotalTokens, @EstimatedCostUsd, @DurationMs,
            true, @CreatedAt, @UpdatedAt)
        RETURNING id
        """;

    public const string InsertToolCall = """
        INSERT INTO aiops.copilot_tool_calls (
            public_id, interaction_id, sequence, tool_name, arguments_json, outcome, summary, duration_ms,
            is_active, created_at, updated_at)
        VALUES (
            @PublicId, @InteractionId, @Sequence, @ToolName, @ArgumentsJson, @Outcome, @Summary, @DurationMs,
            true, @CreatedAt, @UpdatedAt)
        """;

    /// <summary>The newest answered turns of the user's own conversation; the caller reverses them.</summary>
    public const string RecentTurns = """
        SELECT question, answer
        FROM aiops.copilot_interactions
        WHERE conversation_id = @ConversationId AND user_email = @UserEmail AND outcome = 'Answered' AND is_active
        ORDER BY id DESC
        LIMIT @Limit
        """;

    public const string CountInteractions = "SELECT count(*) FROM aiops.copilot_interactions WHERE is_active";

    public const string ListInteractions = """
        SELECT i.public_id AS id, i.conversation_id, i.user_email, i.user_role, i.question, i.outcome, i.failure_reason,
               i.provider, i.model, i.simulated, i.total_tokens, i.estimated_cost_usd, i.duration_ms, i.created_at,
               coalesce(string_agg(t.tool_name, ',' ORDER BY t.sequence), '') AS tools
        FROM aiops.copilot_interactions i
        LEFT JOIN aiops.copilot_tool_calls t ON t.interaction_id = i.id AND t.is_active
        WHERE i.is_active
        GROUP BY i.id
        ORDER BY i.created_at DESC, i.id DESC
        LIMIT @Limit OFFSET @Offset
        """;
}
