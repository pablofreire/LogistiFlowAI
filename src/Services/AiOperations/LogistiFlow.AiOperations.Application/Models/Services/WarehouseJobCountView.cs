namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>Fulfillment jobs of one warehouse.</summary>
public sealed record WarehouseJobCountView(
    string WarehouseCode,
    long Open,
    long Failed,
    long Total);
