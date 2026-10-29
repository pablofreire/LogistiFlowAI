using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.UnitTests.Domain.Entities;

public sealed class ReservationConsumeTests
{
    [Fact]
    public void Consume_ReservedReservation_BecomesConsumedOnce()
    {
        var reservation = ReservationEntity.Reserved(Guid.NewGuid(), "ORD-1", "WH-NJ1", [new(1, "SKU-A", 2)], DateTimeOffset.UnixEpoch);

        reservation.Consume(DateTimeOffset.UnixEpoch).Should().BeTrue();
        reservation.Consume(DateTimeOffset.UnixEpoch).Should().BeFalse();
        reservation.Status.Should().Be(ReservationStatus.Consumed);
    }

    [Fact]
    public void Consume_AfterRelease_IsRefused()
    {
        var reservation = ReservationEntity.Reserved(Guid.NewGuid(), "ORD-1", "WH-NJ1", [new(1, "SKU-A", 2)], DateTimeOffset.UnixEpoch);
        reservation.Release(DateTimeOffset.UnixEpoch);

        reservation.Consume(DateTimeOffset.UnixEpoch).Should().BeFalse();
        reservation.Status.Should().Be(ReservationStatus.Released);
    }

    [Fact]
    public void Release_AfterConsume_IsRefused()
    {
        var reservation = ReservationEntity.Reserved(Guid.NewGuid(), "ORD-1", "WH-NJ1", [new(1, "SKU-A", 2)], DateTimeOffset.UnixEpoch);
        reservation.Consume(DateTimeOffset.UnixEpoch);

        reservation.Release(DateTimeOffset.UnixEpoch).Should().BeFalse();
        reservation.Status.Should().Be(ReservationStatus.Consumed);
    }
}
