namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>Stock of one SKU in one warehouse.</summary>
public sealed record WarehouseStockView(
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available);
