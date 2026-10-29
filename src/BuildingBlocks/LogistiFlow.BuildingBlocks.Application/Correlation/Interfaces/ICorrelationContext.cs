namespace LogistiFlow.BuildingBlocks.Application.Correlation.Interfaces;

/// <summary>
/// Correlation identifier of the current request or message. The HTTP middleware or the message dispatcher
/// sets it, and the outbox copies it into every outgoing event so one business flow can be traced across services.
/// </summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }

    /// <summary>Identifier of the event that caused the current work, when the work was triggered by a message.</summary>
    Guid? CausationId { get; }

    void Set(string correlationId, Guid? causationId = null);
}
