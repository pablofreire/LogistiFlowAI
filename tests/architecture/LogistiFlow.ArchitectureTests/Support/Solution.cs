using System.Reflection;
using LogistiFlow.AiOperations.Api.Composition;
using LogistiFlow.Fulfillment.Api.Composition;
using LogistiFlow.Inventory.Api.Composition;
using LogistiFlow.Orders.Api.Composition;
using LogistiFlow.Shipping.Api.Composition;
using NetArchTest.Rules;

namespace LogistiFlow.ArchitectureTests.Support;

/// <summary>Loads every production assembly once and exposes them by service and layer.</summary>
internal static class Solution
{
    public static readonly string[] Services = ["Orders", "Inventory", "Fulfillment", "Shipping", "AiOperations"];

    public static readonly string[] Layers = ["Domain", "Application", "Infrastructure", "Api"];

    private static readonly Lazy<IReadOnlyList<Assembly>> AllAssemblies = new(LoadAll);

    public static IReadOnlyList<Assembly> Assemblies => AllAssemblies.Value;

    public static Assembly Get(string name) =>
        Assemblies.Single(assembly => assembly.GetName().Name == name);

    public static Assembly Layer(string service, string layer) => Get($"LogistiFlow.{service}.{layer}");

    public static Types AllTypes() => Types.InAssemblies(Assemblies);

    /// <summary>Fails when a rule would pass only because it selected nothing (for example after a rename).</summary>
    public static void AssertRule(PredicateList selection, Func<PredicateList, ConditionList> rule, string because)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(rule);

        selection.GetTypes().Should().NotBeEmpty($"the rule '{because}' must select at least one type");
        var result = rule(selection).GetResult();
        result.IsSuccessful.Should().BeTrue($"{because}; violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    private static List<Assembly> LoadAll()
    {
        var roots = new[] { typeof(OrdersApiMarker).Assembly, typeof(InventoryApiMarker).Assembly, typeof(FulfillmentApiMarker).Assembly, typeof(ShippingApiMarker).Assembly, typeof(AiOperationsApiMarker).Assembly };
        var loaded = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        var pending = new Queue<Assembly>(roots);
        while (pending.Count > 0)
        {
            var assembly = pending.Dequeue();
            var name = assembly.GetName().Name!;
            if (!name.StartsWith("LogistiFlow.", StringComparison.Ordinal) || !loaded.TryAdd(name, assembly))
            {
                continue;
            }

            foreach (var reference in assembly.GetReferencedAssemblies().Where(reference => reference.Name!.StartsWith("LogistiFlow.", StringComparison.Ordinal)))
            {
                pending.Enqueue(Assembly.Load(reference));
            }
        }

        return [.. loaded.Values.OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)];
    }
}
