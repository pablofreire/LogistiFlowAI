namespace LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Contracts;

/// <summary>Inbound units for a SKU at a warehouse. <c>Reference</c> is the receiving document, e.g. a purchase order number.</summary>
public sealed record ReceiveStockRequest(string WarehouseCode, string Sku, int Quantity, string Reference);
