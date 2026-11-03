namespace LogistiFlow.AiOperations.Infrastructure.Providers.Demo.Enums;

/// <summary>The question kinds the demo provider recognises, each answered by fixed tools.</summary>
public enum DemoIntent
{
    Unknown = 0,
    ShipmentDelay = 1,
    ReservationFailure = 2,
    ShipmentsAtRisk = 3,
    WarehouseFailures = 4,
    ExceptionsSummary = 5,
    NextActions = 6,
    FulfillmentStatus = 7,
    OrderStatus = 8,
}
