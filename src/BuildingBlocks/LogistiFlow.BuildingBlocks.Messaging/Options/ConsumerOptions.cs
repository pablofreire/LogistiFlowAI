namespace LogistiFlow.BuildingBlocks.Messaging.Options;

public sealed class ConsumerOptions
{
    public const string SectionName = "Messaging:Consumer";

    private static readonly TimeSpan[] DefaultRetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)];

    public bool Enabled { get; set; } = true;

    public ushort PrefetchCount { get; set; } = 16;

    /// <summary>Total deliveries (first attempt plus retries) before a message goes to the dead-letter queue.</summary>
    public int MaxDeliveryAttempts { get; set; } = 4;

    /// <summary>
    /// Delay of each retry tier. Each tier is a separate queue with a fixed TTL, which gives real backoff
    /// without the head-of-line blocking that per-message TTLs cause in a single queue. Empty means the defaults
    /// (1 s, 5 s, 15 s); the configuration binder would otherwise append configured values to a non-empty default.
    /// </summary>
    public IReadOnlyList<TimeSpan> RetryDelays { get; set; } = [];

    public IReadOnlyList<TimeSpan> EffectiveRetryDelays => RetryDelays.Count > 0 ? RetryDelays : DefaultRetryDelays;
}
