using LogistiFlow.ArchitectureTests.Support;
using NetArchTest.Rules;

namespace LogistiFlow.ArchitectureTests;

/// <summary>Clean Architecture direction inside each service: Api → Infrastructure → Application → Domain.</summary>
public sealed class LayerDependencyTests
{
    public static TheoryData<string> Services() => new(Solution.Services);

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_HasNoFrameworkOrOuterLayerDependencies(string service)
    {
        Solution.AssertRule(
            Types.InAssembly(Solution.Layer(service, "Domain")).That().ResideInNamespaceStartingWith($"LogistiFlow.{service}.Domain"),
            types => types.ShouldNot().HaveDependencyOnAny(
                $"LogistiFlow.{service}.Application",
                $"LogistiFlow.{service}.Infrastructure",
                $"LogistiFlow.{service}.Api",
                "LogistiFlow.BuildingBlocks.Application",
                "LogistiFlow.BuildingBlocks.Infrastructure",
                "Dapper",
                "Npgsql",
                "RabbitMQ",
                "Microsoft.AspNetCore",
                "FluentValidation"),
            $"{service} domain stays persistence- and framework-free");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_DoesNotDependOnInfrastructureOrTransport(string service)
    {
        Solution.AssertRule(
            Types.InAssembly(Solution.Layer(service, "Application")).That().ResideInNamespaceStartingWith($"LogistiFlow.{service}.Application"),
            types => types.ShouldNot().HaveDependencyOnAny(
                $"LogistiFlow.{service}.Infrastructure",
                $"LogistiFlow.{service}.Api",
                "LogistiFlow.BuildingBlocks.Infrastructure",
                "LogistiFlow.BuildingBlocks.Messaging",
                "LogistiFlow.BuildingBlocks.Web",
                "Dapper",
                "Npgsql",
                "RabbitMQ",
                "Microsoft.AspNetCore"),
            $"{service} application depends only on abstractions");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Controllers_DoNotTouchPersistenceOrDomainEntities(string service)
    {
        Solution.AssertRule(
            Types.InAssembly(Solution.Layer(service, "Api")).That().ResideInNamespace($"LogistiFlow.{service}.Api.Controllers"),
            types => types.ShouldNot().HaveDependencyOnAny(
                $"LogistiFlow.{service}.Infrastructure",
                $"LogistiFlow.{service}.Domain.Entities",
                $"LogistiFlow.{service}.Domain.Interfaces",
                "Dapper",
                "Npgsql"),
            $"{service} controllers go through use cases");
    }

    [Fact]
    public void Dapper_IsUsedOnlyInInfrastructureAndMessaging()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().HaveDependencyOn("Dapper"),
            types => types.Should().ResideInNamespaceContaining(".Infrastructure.").Or().ResideInNamespaceStartingWith("LogistiFlow.BuildingBlocks.Messaging"),
            "SQL lives in Infrastructure");
    }
}
