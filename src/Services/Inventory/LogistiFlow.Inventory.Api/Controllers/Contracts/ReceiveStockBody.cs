namespace LogistiFlow.Inventory.Api.Controllers.Contracts;

public sealed record ReceiveStockBody(string WarehouseCode, string Sku, int Quantity, string Reference);
