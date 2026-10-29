namespace LogistiFlow.BuildingBlocks.Web.Security;

public static class LogistiFlowRoles
{
    public const string Admin = "Admin";
    public const string OpsManager = "OpsManager";
    public const string WarehouseOperator = "WarehouseOperator";
    public const string Viewer = "Viewer";

    public static IReadOnlyList<string> All { get; } = [Admin, OpsManager, WarehouseOperator, Viewer];
}
