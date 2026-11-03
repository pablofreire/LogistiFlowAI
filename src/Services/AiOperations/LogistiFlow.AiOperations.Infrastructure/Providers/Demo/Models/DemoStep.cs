namespace LogistiFlow.AiOperations.Infrastructure.Providers.Demo.Models;

/// <summary>The demo provider's next move: run these tools, or answer with this text.</summary>
public sealed record DemoStep(IReadOnlyList<DemoToolCall> Calls, string? Answer)
{
    public static DemoStep Run(params DemoToolCall[] calls) => new(calls, null);

    public static DemoStep Reply(string answer) => new([], answer);
}
