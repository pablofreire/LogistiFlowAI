using System.Security.Claims;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.BuildingBlocks.UnitTests.Web;

/// <summary>The role-to-permission matrix every service enforces.</summary>
public sealed class AuthorizationPolicyTests
{
    private readonly IAuthorizationService _authorization;
    private readonly IAuthorizationPolicyProvider _policies;

    public AuthorizationPolicyTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = new string('k', 48) })
            .Build();
        var services = new ServiceCollection().AddLogging();
        services.AddLogistiFlowJwtAuthentication(configuration);
        var provider = services.BuildServiceProvider();
        _authorization = provider.GetRequiredService<IAuthorizationService>();
        _policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
    }

    [Theory]
    [InlineData(LogistiFlowRoles.Admin, AuthorizationPolicies.ManageOrders, true)]
    [InlineData(LogistiFlowRoles.OpsManager, AuthorizationPolicies.ManageOrders, true)]
    [InlineData(LogistiFlowRoles.WarehouseOperator, AuthorizationPolicies.ManageOrders, false)]
    [InlineData(LogistiFlowRoles.Viewer, AuthorizationPolicies.ManageOrders, false)]
    [InlineData(LogistiFlowRoles.Viewer, AuthorizationPolicies.ManageInventory, false)]
    [InlineData(LogistiFlowRoles.OpsManager, AuthorizationPolicies.ManageInventory, true)]
    [InlineData(LogistiFlowRoles.WarehouseOperator, AuthorizationPolicies.OperateWarehouse, true)]
    [InlineData(LogistiFlowRoles.Viewer, AuthorizationPolicies.OperateWarehouse, false)]
    [InlineData(LogistiFlowRoles.Viewer, AuthorizationPolicies.ReadOperations, true)]
    [InlineData("Intruder", AuthorizationPolicies.ReadOperations, false)]
    public async Task Policy_ForRole_MatchesTheMatrix(string role, string policy, bool allowed)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtAuthenticationServiceCollectionExtensions.RoleClaimType, role)],
            authenticationType: "test",
            nameType: JwtAuthenticationServiceCollectionExtensions.NameClaimType,
            roleType: JwtAuthenticationServiceCollectionExtensions.RoleClaimType));

        var result = await _authorization.AuthorizeAsync(user, policy);

        result.Succeeded.Should().Be(allowed);
    }

    [Fact]
    public async Task FallbackPolicy_RejectsAnonymousUsers()
    {
        var fallback = await _policies.GetFallbackPolicyAsync();

        var result = await _authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, fallback!);

        fallback.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
    }
}
