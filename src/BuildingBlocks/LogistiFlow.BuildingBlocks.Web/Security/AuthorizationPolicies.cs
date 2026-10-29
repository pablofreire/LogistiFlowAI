namespace LogistiFlow.BuildingBlocks.Web.Security;

/// <summary>
/// Permission-oriented policies. Endpoints depend on a policy, never on a role name, so the role-to-permission
/// mapping lives in one place (<see cref="Configuration.JwtAuthenticationServiceCollectionExtensions"/>).
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Read operational data: every signed-in role.</summary>
    public const string ReadOperations = "operations.read";

    /// <summary>Create and cancel orders.</summary>
    public const string ManageOrders = "orders.manage";

    /// <summary>Products, warehouses and stock adjustments.</summary>
    public const string ManageInventory = "inventory.manage";

    /// <summary>Warehouse floor actions such as picking and packing.</summary>
    public const string OperateWarehouse = "warehouse.operate";

    /// <summary>Resolve shipment exceptions and retry failed carrier bookings.</summary>
    public const string ManageShipments = "shipments.manage";
}
