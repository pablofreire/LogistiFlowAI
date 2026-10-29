namespace LogistiFlow.BuildingBlocks.Messaging.Enums;

public enum DispatchOutcome
{
    Handled = 0,

    /// <summary>The inbox already holds this event for this consumer; nothing was applied.</summary>
    Duplicate = 1,

    /// <summary>No handler is registered for the event type and version.</summary>
    Unhandled = 2,
}
