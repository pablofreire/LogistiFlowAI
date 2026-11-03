namespace LogistiFlow.AiOperations.Application.Tools;

/// <summary>The names the model sees and the audit trail records.</summary>
public static class OperationsToolNames
{
    public const string GetOrderDetails = nameof(GetOrderDetails);

    public const string GetInventoryAvailability = nameof(GetInventoryAvailability);

    public const string GetFulfillmentStatus = nameof(GetFulfillmentStatus);

    public const string GetShipmentTracking = nameof(GetShipmentTracking);

    public const string AnalyzeShipmentDelay = nameof(AnalyzeShipmentDelay);

    public const string ListShipmentsAtRisk = nameof(ListShipmentsAtRisk);

    public const string ListOperationalExceptions = nameof(ListOperationalExceptions);

    public const string GetWarehouseMetrics = nameof(GetWarehouseMetrics);

    public const string SuggestRecoveryActions = nameof(SuggestRecoveryActions);

    public static readonly IReadOnlyList<string> All =
    [
        GetOrderDetails,
        GetInventoryAvailability,
        GetFulfillmentStatus,
        GetShipmentTracking,
        AnalyzeShipmentDelay,
        ListShipmentsAtRisk,
        ListOperationalExceptions,
        GetWarehouseMetrics,
        SuggestRecoveryActions,
    ];
}
