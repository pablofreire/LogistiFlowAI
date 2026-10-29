namespace LogistiFlow.Orders.Domain.Enums;

/// <summary>Order lifecycle. Allowed transitions are defined in <see cref="Services.OrderStateMachine"/>.</summary>
public enum OrderStatus
{
    Created = 0,
    PendingInventory = 1,
    Reserved = 2,
    Picking = 3,
    Packed = 4,
    Shipped = 5,
    Delivered = 6,
    Cancelled = 7,
    Failed = 8,
}
