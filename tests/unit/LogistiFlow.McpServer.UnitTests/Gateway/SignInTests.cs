using System.Net;
using LogistiFlow.McpServer.Options;
using LogistiFlow.McpServer.Tools;
using LogistiFlow.McpServer.UnitTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using static LogistiFlow.McpServer.UnitTests.Fixtures.McpTestServer;

namespace LogistiFlow.McpServer.UnitTests.Gateway;

/// <summary>Signing in with e-mail and password: one sign-in per token lifetime, and no credentials in errors.</summary>
public sealed class SignInTests
{
    private const string Email = "viewer@logistiflow.local";
    private const string Password = "unit-test-password";

    private readonly FakeGateway _gateway = new();
    private readonly FakeTimeProvider _time = new(LogisticsData.Now);

    private static Dictionary<string, string?> CredentialSettings() => new(StringComparer.Ordinal)
    {
        [GatewayOptions.BaseUrlVariable] = "http://gateway.test",
        [GatewayOptions.EmailVariable] = Email,
        [GatewayOptions.PasswordVariable] = Password,
    };

    private FakeGateway SignInIssues(string token, TimeSpan lifetime) =>
        _gateway.Ok("/api/auth/login", new { accessToken = token, expiresAt = _time.GetUtcNow().Add(lifetime), user = new { email = Email } }, method: "POST");

    private void InventoryAnswers() => _gateway.Ok("/api/inventory/availability/SKU-1003", LogisticsData.Availability());

    [Fact]
    public async Task SignsInOnce_AndReusesTheTokenWhileItIsValid()
    {
        SignInIssues("token-1", TimeSpan.FromHours(1));
        InventoryAnswers();
        await using var server = await StartAsync(_gateway, CredentialSettings(), _time);

        Structured(await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" }));
        Structured(await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" }));

        var signIn = _gateway.To("/api/auth/login").Should().ContainSingle().Subject;
        signIn.Body.Should().Contain(Email);
        signIn.Authorization.Should().BeNull();
        _gateway.To("/api/inventory/availability/SKU-1003").Should().OnlyContain(request => request.Authorization == "Bearer token-1");
    }

    [Fact]
    public async Task SignsInAgain_ShortlyBeforeTheTokenExpires()
    {
        SignInIssues("token-1", TimeSpan.FromMinutes(30));
        InventoryAnswers();
        await using var server = await StartAsync(_gateway, CredentialSettings(), _time);
        Structured(await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" }));

        _time.Advance(TimeSpan.FromMinutes(29.5));
        SignInIssues("token-2", TimeSpan.FromMinutes(30));
        Structured(await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" }));

        _gateway.To("/api/auth/login").Should().HaveCount(2);
        _gateway.To("/api/inventory/availability/SKU-1003").Select(request => request.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
    }

    [Fact]
    public async Task ATokenTheApiRejects_IsDropped_SoTheNextCallSignsInAgain()
    {
        SignInIssues("revoked", TimeSpan.FromHours(1));
        _gateway.Fail("/api/inventory/availability/SKU-1003", HttpStatusCode.Unauthorized, "Unauthorized.");
        await using var server = await StartAsync(_gateway, CredentialSettings(), _time);

        var first = await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" });
        SignInIssues("fresh", TimeSpan.FromHours(1));
        InventoryAnswers();
        var second = await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" });

        first.IsError.Should().BeTrue();
        Text(first).Should().Contain("did not accept");
        Structured(second).GetProperty("sku").GetString().Should().Be("SKU-1003");
        _gateway.To("/api/inventory/availability/SKU-1003")[^1].Authorization.Should().Be("Bearer fresh");
    }

    [Fact]
    public async Task RejectedCredentials_AreAToolError_ThatNeverEchoesThePassword()
    {
        _gateway.Fail("/api/auth/login", HttpStatusCode.Unauthorized, "Invalid e-mail or password.", method: "POST");
        await using var server = await StartAsync(_gateway, CredentialSettings(), _time);

        var result = await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain($"did not accept the configured credentials for {Email}").And.NotContain(Password);
        _gateway.To("/api/inventory/availability/SKU-1003").Should().BeEmpty();
    }

    [Fact]
    public async Task AConfiguredAccessToken_IsUsedAsIs_WithoutSigningIn()
    {
        InventoryAnswers();
        await using var server = await StartAsync(_gateway);

        var availability = Structured(await server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" }));

        availability.GetProperty("productName").GetString().Should().Be("Barcode scanner");
        _gateway.To("/api/auth/login").Should().BeEmpty();
    }
}
