using System.Runtime.CompilerServices;
using System.Text.Json;
using LogistiFlow.AiOperations.Infrastructure.Providers.Demo;
using Microsoft.Extensions.AI;

namespace LogistiFlow.AiOperations.Infrastructure.Providers;

/// <summary>
/// A deterministic stand-in for a model, used when no AI provider is configured. It speaks the
/// <see cref="IChatClient"/> protocol: it requests tool calls, the agent framework runs the real tools, and it
/// answers from their results with <see cref="DemoCopilotScript"/>. It reports no token usage.
/// </summary>
public sealed class DemoOperationsChatClient : IChatClient
{
    public const string ProviderName = "demo";

    public const string ModelName = "deterministic-demo-v1";

    private static readonly ChatClientMetadata Metadata = new(ProviderName, providerUri: null, defaultModelId: ModelName);

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        cancellationToken.ThrowIfCancellationRequested();

        var conversation = messages.ToList();
        var lastQuestion = conversation.FindLastIndex(message => message.Role == ChatRole.User);
        var questions = conversation.Where(message => message.Role == ChatRole.User).Select(message => message.Text).ToList();
        var thisTurn = lastQuestion < 0 ? conversation : conversation.Skip(lastQuestion + 1).ToList();

        var step = DemoCopilotScript.Next(questions, ResultsByTool(thisTurn));
        var available = options?.Tools?.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal) ?? [];

        ChatMessage reply;
        if (step.Answer is not null)
        {
            reply = new ChatMessage(ChatRole.Assistant, step.Answer);
        }
        else if (step.Calls.All(call => available.Contains(call.Name)))
        {
            var round = thisTurn.Count(message => message.Role == ChatRole.Assistant);
            reply = new ChatMessage(
                ChatRole.Assistant,
                [.. step.Calls.Select((call, index) => (AIContent)new FunctionCallContent($"demo-{round}-{index}", call.Name, call.Arguments.ToDictionary()))]);
        }
        else
        {
            reply = new ChatMessage(ChatRole.Assistant, "The tools this question needs are not available. " + DemoAnswerComposer.SimulatedFooter);
        }

        return Task.FromResult(new ChatResponse(reply) { ModelId = ModelName });
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return serviceKey is null && serviceType.IsInstanceOfType(Metadata) ? Metadata
            : serviceKey is null && serviceType.IsInstanceOfType(this) ? this
            : null;
    }

    public void Dispose()
    {
        // Nothing to release.
    }

    /// <summary>Results of this turn's tool calls, by tool name, in the order they came back.</summary>
    private static Dictionary<string, IReadOnlyList<JsonElement>> ResultsByTool(List<ChatMessage> thisTurn)
    {
        var names = thisTurn.SelectMany(message => message.Contents).OfType<FunctionCallContent>()
            .ToDictionary(call => call.CallId, call => call.Name, StringComparer.Ordinal);
        var results = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
        foreach (var result in thisTurn.SelectMany(message => message.Contents).OfType<FunctionResultContent>())
        {
            if (!names.TryGetValue(result.CallId, out var name))
            {
                continue;
            }

            var element = result.Result is JsonElement json ? json : JsonSerializer.SerializeToElement(result.Result);
            (results.TryGetValue(name, out var list) ? list : results[name] = []).Add(element);
        }

        return results.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<JsonElement>)pair.Value, StringComparer.Ordinal);
    }
}
