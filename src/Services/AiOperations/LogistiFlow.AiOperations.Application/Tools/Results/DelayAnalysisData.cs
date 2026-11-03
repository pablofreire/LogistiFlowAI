using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>The delay facts of one shipment. <c>DaysLateVersusPromise</c> compares the current estimate (or the delivery) with the promised date.</summary>
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
    IReadOnlyList<ShipmentExceptionView> OpenExceptions,
    TrackingEventView? LatestEvent,
    string Assessment);
