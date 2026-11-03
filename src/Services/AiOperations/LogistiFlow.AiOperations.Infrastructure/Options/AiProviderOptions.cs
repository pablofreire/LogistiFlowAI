namespace LogistiFlow.AiOperations.Infrastructure.Options;

/// <summary>
/// An OpenAI-compatible chat endpoint (OpenAI, or Azure AI Foundry's OpenAI v1 endpoint). When <c>ApiKey</c> or
/// <c>Model</c> is empty the copilot uses the deterministic demo provider. The key comes from the environment or a
/// secret store, never from a committed file.
/// </summary>
public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    /// <summary>Null for api.openai.com; for Azure, <c>https://{resource}.openai.azure.com/openai/v1/</c>.</summary>
    public Uri? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? Model { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model);
}
