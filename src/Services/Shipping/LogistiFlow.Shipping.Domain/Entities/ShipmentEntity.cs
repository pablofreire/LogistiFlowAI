using System.Globalization;
using LogistiFlow.BuildingBlocks.Domain.Entities;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Errors;
using LogistiFlow.Shipping.Domain.Models;
using LogistiFlow.Shipping.Domain.Services;

namespace LogistiFlow.Shipping.Domain.Entities;

/// <summary>
/// One parcel handed to a carrier for one order. Booking and tracking results from the carrier change it; delays and
/// failures open <see cref="ShipmentExceptionEntity"/> records that delivery or an operator resolves.
/// </summary>
public sealed class ShipmentEntity : Entity
{
    public const string SystemActor = "system";

    private readonly List<ShipmentExceptionEntity> _exceptions;

    private ShipmentEntity(
        Guid orderId,
        string orderNumber,
        Guid fulfillmentJobId,
        string warehouseCode,
        string destinationCity,
        string destinationCountry,
        DateOnly? promisedDeliveryDate,
        string carrierCode,
        List<ShipmentExceptionEntity> exceptions)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        FulfillmentJobId = fulfillmentJobId;
        WarehouseCode = warehouseCode;
        DestinationCity = destinationCity;
        DestinationCountry = destinationCountry;
        PromisedDeliveryDate = promisedDeliveryDate;
        CarrierCode = carrierCode;
        _exceptions = exceptions;
    }

    public Guid OrderId { get; }

    public string OrderNumber { get; }

    public Guid FulfillmentJobId { get; }

    public string WarehouseCode { get; }

    public string DestinationCity { get; }

    public string DestinationCountry { get; }

    public DateOnly? PromisedDeliveryDate { get; }

    public string CarrierCode { get; }

    public string? TrackingNumber { get; private set; }

    /// <summary>The carrier's first estimate, kept to measure delays against.</summary>
    public DateOnly? OriginalEstimatedDelivery { get; private set; }

    public DateOnly? EstimatedDelivery { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public string? StatusReason { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyList<ShipmentExceptionEntity> Exceptions => _exceptions;

    public bool IsDelayed => _exceptions.Exists(exception => exception.Type == ShipmentExceptionType.Delay && exception.IsOpen);

    public static ShipmentEntity CreatePendingBooking(
        OrderDestination destination,
        Guid fulfillmentJobId,
        string warehouseCode,
        DateTimeOffset shippedAt,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var carrier = CarrierSelectionPolicy.Select(
            destination.DestinationCountry,
            destination.PromisedDeliveryDate,
            DateOnly.FromDateTime(shippedAt.UtcDateTime));
        var shipment = new ShipmentEntity(
            destination.OrderId,
            destination.OrderNumber,
            fulfillmentJobId,
            warehouseCode,
            destination.DestinationCity,
            destination.DestinationCountry,
            destination.PromisedDeliveryDate,
            carrier,
            [])
        {
            Status = ShipmentStatus.PendingBooking,
            StatusReason = $"Waiting for {carrier} booking",
            Version = 1,
        };
        shipment.Initialize(now);
        return shipment;
    }

    public static ShipmentEntity Restore(
        long id,
        Guid publicId,
        Guid orderId,
        string orderNumber,
        Guid fulfillmentJobId,
        string warehouseCode,
        string destinationCity,
        string destinationCountry,
        DateOnly? promisedDeliveryDate,
        string carrierCode,
        string? trackingNumber,
        DateOnly? originalEstimatedDelivery,
        DateOnly? estimatedDelivery,
        ShipmentStatus status,
        string? statusReason,
        DateTimeOffset? deliveredAt,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<ShipmentExceptionEntity> exceptions)
    {
        var shipment = new ShipmentEntity(
            orderId,
            orderNumber,
            fulfillmentJobId,
            warehouseCode,
            destinationCity,
            destinationCountry,
            promisedDeliveryDate,
            carrierCode,
            exceptions.ToList())
        {
            TrackingNumber = trackingNumber,
            OriginalEstimatedDelivery = originalEstimatedDelivery,
            EstimatedDelivery = estimatedDelivery,
            Status = status,
            StatusReason = statusReason,
            DeliveredAt = deliveredAt,
            Version = version,
        };
        shipment.Restore(id, publicId, isActive: true, createdAt, updatedAt);
        return shipment;
    }

    /// <summary>The carrier accepted the booking. A carrier estimate after the promised date is a delay from day one.</summary>
    public Result MarkBooked(string trackingNumber, DateOnly estimatedDelivery, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        if (!ShipmentStateMachine.CanTransition(Status, ShipmentStatus.LabelCreated))
        {
            return ShippingErrors.InvalidTransition(Status, ShipmentStatus.LabelCreated);
        }

        TrackingNumber = trackingNumber;
        OriginalEstimatedDelivery = estimatedDelivery;
        EstimatedDelivery = estimatedDelivery;
        SetStatus(ShipmentStatus.LabelCreated, $"Booked with {CarrierCode}, tracking {trackingNumber}", now);
        ResolveOpen(ShipmentExceptionType.CarrierUnavailable, "Booking succeeded", SystemActor, now);

        if (PromisedDeliveryDate is { } promised && estimatedDelivery > promised)
        {
            OpenOrUpdate(ShipmentExceptionType.Delay, $"Carrier estimate {Format(estimatedDelivery)} is after the promised date {Format(promised)}", now);
        }

        return Result.Success();
    }

    /// <summary>Records a failed booking call. After <paramref name="maxAttempts"/> the shipment needs an operator.</summary>
    public void RecordBookingFailure(string error, int attempt, int maxAttempts, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        var details = string.Create(CultureInfo.InvariantCulture, $"{CarrierCode} booking attempt {attempt} of {maxAttempts} failed: {error}");
        OpenOrUpdate(ShipmentExceptionType.CarrierUnavailable, details, now);

        if (attempt >= maxAttempts && Status == ShipmentStatus.PendingBooking)
        {
            SetStatus(ShipmentStatus.BookingFailed, details, now);
        }
        else
        {
            StatusReason = details.Length <= 500 ? details : details[..500];
            Touch(now);
        }
    }

    public Result RetryBooking(DateTimeOffset now)
    {
        if (!ShipmentStateMachine.CanTransition(Status, ShipmentStatus.PendingBooking))
        {
            return ShippingErrors.InvalidTransition(Status, ShipmentStatus.PendingBooking);
        }

        SetStatus(ShipmentStatus.PendingBooking, $"Booking retry requested for {CarrierCode}", now);
        return Result.Success();
    }

    /// <summary>
    /// Applies one new carrier event. Returns <c>true</c> when the status changed. Events that would move the shipment
    /// backwards are kept in the history but do not change the status.
    /// </summary>
    public bool ApplyTrackingEvent(CarrierTrackingEvent trackingEvent, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(trackingEvent);

        if (trackingEvent.EstimatedDelivery is { } estimate)
        {
            if (trackingEvent.Code == "DELAYED" || (EstimatedDelivery is { } current && estimate > current))
            {
                OpenOrUpdate(ShipmentExceptionType.Delay, DescribeDelay(trackingEvent.Description, estimate), now);
            }

            EstimatedDelivery = estimate;
        }

        switch (trackingEvent.Code)
        {
            case "DELIVERY_ATTEMPT_FAILED":
                OpenOrUpdate(ShipmentExceptionType.DeliveryAttemptFailed, trackingEvent.Description, now);
                break;
            case "DELIVERED":
                DeliveredAt = trackingEvent.OccurredAt;
                ResolveOpen(ShipmentExceptionType.Delay, $"Delivered on {Format(DateOnly.FromDateTime(trackingEvent.OccurredAt.UtcDateTime))}", SystemActor, now);
                ResolveOpen(ShipmentExceptionType.DeliveryAttemptFailed, "Delivered on a later attempt", SystemActor, now);
                break;
            case "RETURNED_TO_SENDER":
                ResolveOpen(ShipmentExceptionType.DeliveryAttemptFailed, "Superseded by the return to sender", SystemActor, now);
                ResolveOpen(ShipmentExceptionType.Delay, "Superseded by the return to sender", SystemActor, now);
                OpenOrUpdate(ShipmentExceptionType.ReturnedToSender, trackingEvent.Description, now);
                break;
        }

        var target = ShipmentStateMachine.StatusForCarrierEvent(trackingEvent.Code);
        if (target is not { } status || status == Status || !ShipmentStateMachine.CanTransition(Status, status))
        {
            Touch(now);
            return false;
        }

        SetStatus(status, trackingEvent.Description, now);
        return true;
    }

    public Result ResolveException(Guid exceptionId, string resolution, string resolvedBy, DateTimeOffset now)
    {
        var exception = _exceptions.Find(candidate => candidate.PublicId == exceptionId);
        if (exception is null)
        {
            return ShippingErrors.ExceptionNotFound;
        }

        if (!exception.IsOpen)
        {
            return ShippingErrors.ExceptionAlreadyResolved;
        }

        exception.Resolve(resolution, resolvedBy, now);
        Touch(now);
        return Result.Success();
    }

    public void MarkPersisted(int storedVersion)
    {
        Version = storedVersion;
        foreach (var exception in _exceptions)
        {
            exception.MarkPersisted();
        }
    }

    private string DescribeDelay(string carrierText, DateOnly estimate)
    {
        var description = $"{carrierText}. Estimate moved to {Format(estimate)}";
        if (OriginalEstimatedDelivery is { } original)
        {
            description += $" (was {Format(original)})";
        }

        if (PromisedDeliveryDate is { } promised && estimate > promised)
        {
            description += $"; promised {Format(promised)}";
        }

        return description;
    }

    private void OpenOrUpdate(ShipmentExceptionType type, string details, DateTimeOffset now)
    {
        var open = _exceptions.Find(exception => exception.Type == type && exception.IsOpen);
        if (open is null)
        {
            _exceptions.Add(ShipmentExceptionEntity.Open(type, details, now));
        }
        else if (open.Details != details)
        {
            open.UpdateDetails(details, now);
        }
    }

    private void ResolveOpen(ShipmentExceptionType type, string resolution, string resolvedBy, DateTimeOffset now)
    {
        foreach (var exception in _exceptions.Where(exception => exception.Type == type && exception.IsOpen))
        {
            exception.Resolve(resolution, resolvedBy, now);
        }
    }

    private void SetStatus(ShipmentStatus status, string reason, DateTimeOffset now)
    {
        Status = status;
        StatusReason = reason.Length <= 500 ? reason : reason[..500];
        Touch(now);
    }

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
