using System.ClientModel;
using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Domain.Models;
using LogistiFlow.AiOperations.Infrastructure.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace LogistiFlow.AiOperations.Infrastructure.Providers;

/// <summary>
/// Chooses the model once, from configuration: an OpenAI-compatible endpoint when a key and a model are set,
/// otherwise the deterministic demo provider, which is always reported as simulated.
/// </summary>
public sealed class CopilotChatClientFactory(IOptions<AiProviderOptions> options) : ICopilotChatClientFactory
{
    private readonly AiProviderOptions _settings = options.Value;

    // An empty setting binds to an empty relative URI; only an absolute one is an endpoint.
    private Uri? Endpoint => _settings.Endpoint is { IsAbsoluteUri: true } endpoint ? endpoint : null;

    public CopilotProvider Provider => _settings.IsConfigured
        ? new CopilotProvider(ProviderName(Endpoint), _settings.Model!, Simulated: false)
        : new CopilotProvider(DemoOperationsChatClient.ProviderName, DemoOperationsChatClient.ModelName, Simulated: true);

    public IChatClient Create()
    {
        if (!_settings.IsConfigured)
        {
            return new DemoOperationsChatClient();
        }

        var clientOptions = new OpenAIClientOptions();
        if (Endpoint is { } endpoint)
        {
            clientOptions.Endpoint = endpoint;
        }

        return new ChatClient(_settings.Model, new ApiKeyCredential(_settings.ApiKey!), clientOptions).AsIChatClient();
    }

    private static string ProviderName(Uri? endpoint) =>
        endpoint is null ? "openai"
        : endpoint.Host.EndsWith(".azure.com", StringComparison.OrdinalIgnoreCase) ? "azure-openai"
        : "openai-compatible";
}
