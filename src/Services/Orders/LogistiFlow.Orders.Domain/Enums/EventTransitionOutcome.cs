namespace LogistiFlow.Orders.Domain.Enums;

/// <summary>What happened when an integration event tried to move an order to a new status.</summary>
public enum EventTransitionOutcome
{
    /// <summary>The order moved to the target status.</summary>
    Applied = 0,

    /// <summary>The order is already at, past, or terminally away from the target status; the event is ignored.</summary>
    Stale = 1,

    /// <summary>The order has not reached the prerequisite status yet (the event arrived early); retry later.</summary>
    NotYetApplicable = 2,
}
