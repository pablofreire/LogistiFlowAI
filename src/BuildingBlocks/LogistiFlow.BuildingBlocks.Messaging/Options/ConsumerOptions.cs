namespace LogistiFlow.BuildingBlocks.Messaging.Options;

public sealed class ConsumerOptions
{
    public const string SectionName = "Messaging:Consumer";

    public bool Enabled { get; set; } = true;

    public ushort PrefetchCount { get; set; } = 16;

    /// <summary>Total deliveries (first attempt plus retries) before a message goes to the dead-letter queue.</summary>
    public int MaxDeliveryAttempts { get; set; } = 4;

    /// <summary>
    /// Delay of each retry tier. Each tier is a separate queue with a fixed TTL, which gives real backoff
    /// without the head-of-line blocking that per-message TTLs cause in a single queue.
    /// </summary>
    public IReadOnlyList<TimeSpan> RetryDelays { get; set; } = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)];
}
