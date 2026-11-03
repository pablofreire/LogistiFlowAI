using LogistiFlow.ArchitectureTests.Support;
using LogistiFlow.CarrierSimulator.Composition;
using LogistiFlow.McpServer.Composition;

namespace LogistiFlow.ArchitectureTests;

/// <summary>Microservice boundaries: services share only versioned event contracts and generic building blocks.</summary>
public sealed class ServiceBoundaryTests
{
    public static TheoryData<string, string> ServicePairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var service in Solution.Services)
        {
            foreach (var other in Solution.Services.Where(other => other != service))
            {
                data.Add(service, other);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ServicePairs))]
    public void Service_DoesNotReferenceAnotherService(string service, string other)
    {
        var references = Solution.Layers
            .Select(layer => Solution.Layer(service, layer))
            .SelectMany(assembly => assembly.GetReferencedAssemblies())
            .Select(reference => reference.Name!);

        references.Should().NotContain(name => name.StartsWith($"LogistiFlow.{other}.", StringComparison.Ordinal));
    }

    [Fact]
    public void IntegrationEventContracts_DependOnNothingFromTheSolution()
    {
        Solution.Get("LogistiFlow.Contracts.IntegrationEvents").GetReferencedAssemblies()
            .Should().NotContain(reference => reference.Name!.StartsWith("LogistiFlow.", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildingBlocks_DoNotDependOnAnyService()
    {
        var buildingBlocks = Solution.Assemblies.Where(assembly => assembly.GetName().Name!.StartsWith("LogistiFlow.BuildingBlocks.", StringComparison.Ordinal)).ToList();

        buildingBlocks.Should().NotBeEmpty();
        buildingBlocks.SelectMany(assembly => assembly.GetReferencedAssemblies())
            .Should().NotContain(reference => Solution.Services.Any(service => reference.Name!.StartsWith($"LogistiFlow.{service}.", StringComparison.Ordinal)));
    }

    /// <summary>The carrier simulator stands in for a third party: it must not share code with LogistiFlow.</summary>
    [Fact]
    public void CarrierSimulator_ReferencesNoLogistiFlowAssembly()
    {
        var simulator = typeof(CarrierSimulatorMarker).Assembly;

        simulator.GetName().Name.Should().Be("LogistiFlow.CarrierSimulator");
        simulator.GetReferencedAssemblies()
            .Should().NotContain(reference => reference.Name!.StartsWith("LogistiFlow.", StringComparison.Ordinal));
    }

    /// <summary>The MCP server is a gateway client: it reaches the platform over HTTP only and shares no code with it.</summary>
    [Fact]
    public void McpServer_ReferencesNoLogistiFlowAssembly()
    {
        var server = typeof(McpServerServiceCollectionExtensions).Assembly;

        server.GetName().Name.Should().Be("LogistiFlow.McpServer");
        server.GetReferencedAssemblies()
            .Should().NotContain(reference => reference.Name!.StartsWith("LogistiFlow.", StringComparison.Ordinal));
    }
}
