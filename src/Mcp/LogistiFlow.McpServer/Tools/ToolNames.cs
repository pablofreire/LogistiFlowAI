namespace LogistiFlow.McpServer.Tools;

/// <summary>The tool names MCP clients see. They are a public contract: renaming one breaks every client prompt.</summary>
public static class ToolNames
{
    public const string GetOrder = "logistics_get_order";
    public const string GetInventory = "logistics_get_inventory";
    public const string GetShipment = "logistics_get_shipment";
    public const string ListExceptions = "logistics_list_exceptions";
    public const string GetWarehouseMetrics = "logistics_get_warehouse_metrics";

    public static readonly IReadOnlyList<string> All = [GetOrder, GetInventory, GetShipment, ListExceptions, GetWarehouseMetrics];
}
