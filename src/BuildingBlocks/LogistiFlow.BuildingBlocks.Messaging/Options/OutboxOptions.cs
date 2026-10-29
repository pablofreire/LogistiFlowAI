namespace LogistiFlow.BuildingBlocks.Messaging.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Messaging:Outbox";

    public bool RelayEnabled { get; set; } = true;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(250);

    public int BatchSize { get; set; } = 50;

    /// <summary>How long a claimed row stays invisible to other relay instances.</summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Upper bound of the exponential backoff applied after a failed publish.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(60);
}
