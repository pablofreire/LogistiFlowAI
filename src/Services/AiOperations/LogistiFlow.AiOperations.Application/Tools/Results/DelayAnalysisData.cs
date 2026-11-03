using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>The delay facts of one shipment, with all its exceptions (open and resolved), so a delay is explained after delivery too. <c>DaysLateVersusPromise</c> compares the current estimate (or the delivery) with the promised date.</summary>
public sealed record DelayAnalysisData(
    string OrderNumber,
    string ShipmentStatus,
    string CarrierName,
    string? TrackingNumber,
    DateOnly? PromisedDeliveryDate,
    DateOnly? OriginalEstimatedDelivery,
    DateOnly? EstimatedDelivery,
    DateTimeOffset? DeliveredAt,
    int? DaysLateVersusPromise,
    bool IsDelayed,
    IReadOnlyList<ShipmentExceptionView> Exceptions,
    TrackingEventView? LatestEvent,
    string Assessment);
