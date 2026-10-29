using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.Inventory.Domain.Errors;

public static class InventoryErrors
{
    public const string InsufficientInventoryReason = "Insufficient inventory";
    public const string SplitAcrossWarehousesReason = "No single warehouse holds every line";

    public static readonly Error ProductNotFound = Error.NotFound("inventory.product_not_found", "The product does not exist.");

    public static readonly Error WarehouseNotFound = Error.NotFound("inventory.warehouse_not_found", "The warehouse does not exist.");

    public static readonly Error ReservationNotFound = Error.NotFound("inventory.reservation_not_found", "No reservation exists for the order.");

    public static readonly Error SkuAlreadyExists = Error.Conflict("inventory.sku_exists", "A product with this SKU already exists.");
}
