using FluentValidation;
using LogistiFlow.ArchitectureTests.Support;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Entities;
using LogistiFlow.BuildingBlocks.Web.Api;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.ArchitectureTests;

/// <summary>One artifact kind per folder, suffixes, and sealed concrete types.</summary>
public sealed class ConventionTests
{
    [Fact]
    public void Interfaces_LiveInInterfacesFolders()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().AreInterfaces().And().ResideInNamespaceStartingWith("LogistiFlow."),
            types => types.Should().ResideInNamespaceEndingWith(".Interfaces").Or().ResideInNamespaceContaining(".Interfaces."),
            "an interface never shares a folder with a class");
    }

    [Fact]
    public void UseCaseImplementations_AreSealedSuffixedAndInImplementations()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().ImplementInterface(typeof(IUseCase<,>)).And().AreClasses(),
            types => types.Should().HaveNameEndingWith("UseCase").And().BeSealed().And().ResideInNamespaceEndingWith(".Implementations"),
            "use cases follow UseCases/{Aggregate}/{Action}/Implementations");
    }

    [Fact]
    public void Validators_AreSuffixedAndInValidators()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().Inherit(typeof(AbstractValidator<>)),
            types => types.Should().HaveNameEndingWith("RequestValidator").And().ResideInNamespaceEndingWith(".Validators"),
            "validators live in Validators and end with RequestValidator");
    }

    [Theory]
    [InlineData("Request")]
    [InlineData("Response")]
    public void UseCaseRequestAndResponseTypes_LiveInContracts(string suffix)
    {
        Solution.AssertRule(
            Solution.AllTypes().That().HaveNameEndingWith(suffix).And().ResideInNamespaceContaining(".UseCases."),
            types => types.Should().ResideInNamespaceEndingWith(".Contracts"),
            $"use-case *{suffix} models live in Contracts");
    }

    [Fact]
    public void Entities_AreSuffixedAndInDomainEntities()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().Inherit(typeof(Entity)),
            types => types.Should().HaveNameEndingWith("Entity").And().ResideInNamespaceEndingWith(".Domain.Entities").And().BeSealed(),
            "aggregates are sealed *Entity types in Domain.Entities");
    }

    [Fact]
    public void Repositories_AreImplementedInInfrastructure()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().HaveNameEndingWith("Repository").And().AreClasses(),
            types => types.Should().ResideInNamespaceEndingWith(".Infrastructure.Persistence.Repositories").And().BeSealed(),
            "repository implementations live in Infrastructure.Persistence.Repositories");
    }

    [Fact]
    public void Controllers_InheritTheBaseControllerAndAreSealed()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().Inherit(typeof(ControllerBase)).And().AreNotAbstract(),
            types => types.Should().Inherit(typeof(BaseApiController)).And().BeSealed().And().HaveNameEndingWith("Controller").And().ResideInNamespaceEndingWith(".Api.Controllers"),
            "controllers are thin wrappers over BaseApiController");
    }

    [Fact]
    public void IntegrationEventHandlers_AreSuffixedAndInTheirFolder()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().ImplementInterface(typeof(IIntegrationEventHandler<>)),
            types => types.Should().HaveNameEndingWith("EventHandler").And().BeSealed().And().ResideInNamespaceEndingWith(".Application.IntegrationEventHandlers"),
            "event handlers live in Application.IntegrationEventHandlers");
    }

    [Fact]
    public void Options_LiveInOptionsFolders()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().HaveNameEndingWith("Options").And().AreClasses(),
            types => types.Should().ResideInNamespaceEndingWith(".Options"),
            "options classes live in Options");
    }

    [Fact]
    public void Exceptions_LiveInExceptionsFolders()
    {
        Solution.AssertRule(
            Solution.AllTypes().That().Inherit(typeof(Exception)),
            types => types.Should().ResideInNamespaceEndingWith(".Exceptions"),
            "exceptions live in Exceptions");
    }
}
