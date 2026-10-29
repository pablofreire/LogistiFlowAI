namespace LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;

/// <summary>
/// Thrown by a handler when an event cannot be applied yet, for example because it arrived before the event
/// it depends on. The message is retried with backoff and dead-lettered only after the last attempt.
/// </summary>
public sealed class TransientIntegrationEventException : Exception
{
    public TransientIntegrationEventException()
    {
    }

    public TransientIntegrationEventException(string message)
        : base(message)
    {
    }

    public TransientIntegrationEventException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
