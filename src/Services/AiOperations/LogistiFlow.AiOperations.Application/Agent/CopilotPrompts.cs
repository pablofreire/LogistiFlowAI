namespace LogistiFlow.AiOperations.Application.Agent;

/// <summary>The agent's instructions and the questions offered to users.</summary>
public static class CopilotPrompts
{
    public const string AgentName = "LogistiFlowOperationsCopilot";

    public const string Instructions = """
        You are the LogistiFlow operations copilot for warehouse and logistics operators.
        Answer only from the results of the tools you call in this conversation. Never invent orders, dates,
        quantities, carriers or reasons. If a tool returns a status other than "ok", say what could not be read and
        why (for example "not_found" or "forbidden"); do not guess the missing data.
        You cannot change anything: you read data and suggest next steps an authorised operator may take in the
        console. Never say an action was taken.
        Mention order numbers, tracking numbers and dates exactly as the tools return them. Keep answers short and
        concrete, in English.
        """;

    public static readonly IReadOnlyList<string> SampleQuestions =
    [
        "Why is order ORD-1025 delayed?",
        "Show shipments at risk of missing their delivery deadline.",
        "Which warehouse has the highest number of fulfillment failures?",
        "Summarize operational exceptions from the last 24 hours.",
        "What caused the inventory reservation failure for ORD-1025?",
        "Suggest the next actions for delayed shipments.",
    ];
}
