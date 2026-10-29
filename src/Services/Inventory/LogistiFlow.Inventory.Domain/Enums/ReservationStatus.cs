namespace LogistiFlow.Inventory.Domain.Enums;

public enum ReservationStatus
{
    /// <summary>Stock is held for the order.</summary>
    Reserved = 0,

    /// <summary>The hold was returned to available stock (the order was cancelled after reserving).</summary>
    Released = 1,

    /// <summary>No warehouse could fulfil every line; nothing is held.</summary>
    Failed = 2,

    /// <summary>
    /// Tombstone: the order was cancelled before its reservation was attempted. A late <c>OrderCreated</c> sees it
    /// and reserves nothing.
    /// </summary>
    Cancelled = 3,
}
