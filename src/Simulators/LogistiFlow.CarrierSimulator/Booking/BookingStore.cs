using System.Collections.Concurrent;
using System.Globalization;

namespace LogistiFlow.CarrierSimulator.Booking;

/// <summary>
/// In-memory bookings, keyed by the client's <c>Idempotency-Key</c> and by tracking number. A simulator restart
/// forgets them; LogistiFlow then sees "tracking not found" and keeps retrying, which is documented.
/// </summary>
public sealed class BookingStore
{
    private readonly ConcurrentDictionary<string, CarrierBooking> _byKey = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, CarrierBooking> _byTracking = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _attemptsByKey = new(StringComparer.Ordinal);
    private long _sequence = 10_000_000;

    /// <summary>Every booking call for a key, including rejected and replayed ones.</summary>
    public int RegisterAttempt(string idempotencyKey) => _attemptsByKey.AddOrUpdate(idempotencyKey, 1, (_, count) => count + 1);

    public int AttemptsFor(string idempotencyKey) => _attemptsByKey.GetValueOrDefault(idempotencyKey);

    /// <summary>Distinct bookings created for a reference; the timeout scenario proves this stays at one.</summary>
    public int BookingsForReference(string reference) => _byKey.Values.Count(booking => booking.Reference == reference);

    public bool TryGetByKey(string idempotencyKey, out CarrierBooking booking) => _byKey.TryGetValue(idempotencyKey, out booking!);

    public bool TryGetByTracking(string trackingNumber, out CarrierBooking booking) => _byTracking.TryGetValue(trackingNumber, out booking!);

    /// <summary>Returns the existing booking for the key, or stores the one built by <paramref name="create"/>.</summary>
    public (CarrierBooking Booking, bool Created) GetOrCreate(string idempotencyKey, Func<string, CarrierBooking> create)
    {
        ArgumentNullException.ThrowIfNull(create);

        var created = false;
        var booking = _byKey.GetOrAdd(idempotencyKey, _ =>
        {
            created = true;
            var trackingNumber = Interlocked.Increment(ref _sequence).ToString(CultureInfo.InvariantCulture);
            return create(trackingNumber);
        });

        if (created)
        {
            _byTracking[booking.TrackingNumber] = booking;
        }

        return (booking, created);
    }
}
