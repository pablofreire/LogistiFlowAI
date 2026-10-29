namespace LogistiFlow.Contracts.IntegrationEvents.Models;

public sealed record InventoryShortage(string Sku, int Requested, int Available);
