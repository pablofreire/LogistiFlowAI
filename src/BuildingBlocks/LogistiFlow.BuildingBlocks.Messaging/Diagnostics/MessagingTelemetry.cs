using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LogistiFlow.BuildingBlocks.Messaging.Diagnostics;

/// <summary>Traces and metrics for publishing and consuming integration events.</summary>
public static class MessagingTelemetry
{
    public const string SourceName = "LogistiFlow.Messaging";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> Published = Meter.CreateCounter<long>("logistiflow.messaging.published", description: "Integration events confirmed by the broker.");

    public static readonly Counter<long> PublishFailed = Meter.CreateCounter<long>("logistiflow.messaging.publish_failed", description: "Outbox publish attempts that failed.");

    public static readonly Counter<long> Consumed = Meter.CreateCounter<long>("logistiflow.messaging.consumed", description: "Integration events handled successfully.");

    public static readonly Counter<long> Duplicates = Meter.CreateCounter<long>("logistiflow.messaging.duplicates", description: "Redelivered events skipped by the inbox.");

    public static readonly Counter<long> Retried = Meter.CreateCounter<long>("logistiflow.messaging.retried", description: "Events sent to a retry queue.");

    public static readonly Counter<long> DeadLettered = Meter.CreateCounter<long>("logistiflow.messaging.dead_lettered", description: "Events sent to the dead-letter queue.");
}
