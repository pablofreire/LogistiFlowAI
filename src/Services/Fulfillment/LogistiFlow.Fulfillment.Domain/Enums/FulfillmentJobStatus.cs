namespace LogistiFlow.Fulfillment.Domain.Enums;

public enum FulfillmentJobStatus
{
    Created = 0,
    Picking = 1,
    Packed = 2,
    Shipped = 3,
    Cancelled = 4,
    Failed = 5,
}
