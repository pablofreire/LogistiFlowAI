namespace LogistiFlow.Shipping.Domain.Models;

/// <summary>
/// When the background workers should look at a shipment next. Delays are relative so the repository can add them
/// to the database clock; <c>null</c> means "not scheduled".
/// </summary>
public sealed record ShipmentSchedule(TimeSpan? NextBookingIn, TimeSpan? NextPollIn)
{
    public static ShipmentSchedule None { get; } = new(null, null);

    public static ShipmentSchedule BookIn(TimeSpan delay) => new(delay, null);

    public static ShipmentSchedule PollIn(TimeSpan delay) => new(null, delay);
}
