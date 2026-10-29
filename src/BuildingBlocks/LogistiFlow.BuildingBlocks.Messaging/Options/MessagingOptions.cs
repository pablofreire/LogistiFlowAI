namespace LogistiFlow.BuildingBlocks.Messaging.Options;

public sealed class MessagingOptions
{
    /// <summary>Logical name of the owning service; prefixes its queues and identifies it as an inbox consumer.</summary>
    public string ServiceName { get; set; } = string.Empty;

    public string QueueName => $"{ServiceName}.integration-events";
}
