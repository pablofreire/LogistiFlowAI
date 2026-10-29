using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.UnitTests.Domain.Entities;

public sealed class ReservationEntityTests
{
    [Fact]
    public void Release_ReservedReservation_BecomesReleasedOnce()
    {
        var reservation = ReservationEntity.Reserved(Guid.NewGuid(), "ORD-1", "WH-NJ1", [new(1, "SKU-A", 1)], DateTimeOffset.UnixEpoch);

        reservation.Release(DateTimeOffset.UnixEpoch).Should().BeTrue();
        reservation.Release(DateTimeOffset.UnixEpoch).Should().BeFalse();
        reservation.Status.Should().Be(ReservationStatus.Released);
    }

    [Theory]
    [InlineData(ReservationStatus.Failed)]
    [InlineData(ReservationStatus.Cancelled)]
    public void Release_ReservationThatHoldsNothing_IsANoOp(ReservationStatus status)
    {
        var reservation = status == ReservationStatus.Failed
            ? ReservationEntity.Failed(Guid.NewGuid(), "ORD-1", "Insufficient inventory", DateTimeOffset.UnixEpoch)
            : ReservationEntity.CancelledBeforeReservation(Guid.NewGuid(), "ORD-1", DateTimeOffset.UnixEpoch);

        reservation.Release(DateTimeOffset.UnixEpoch).Should().BeFalse();
        reservation.Status.Should().Be(status);
        reservation.Lines.Should().BeEmpty();
    }
}
