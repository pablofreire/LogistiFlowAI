namespace LogistiFlow.BuildingBlocks.Messaging.Models;

public static class MessageHeaders
{
    public const string Attempt = "x-attempt";
    public const string TraceParent = "traceparent";
    public const string EventVersion = "x-event-version";
    public const string DeadLetterReason = "x-dead-letter-reason";
}
