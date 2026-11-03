using LogistiFlow.McpServer.Options;
using LogistiFlow.McpServer.UnitTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LogistiFlow.McpServer.UnitTests.Options;

public sealed class GatewayOptionsTests
{
    [Fact]
    public async Task ServerRefusesToStart_WithoutATokenOrCredentials()
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal) { [GatewayOptions.EmailVariable] = "viewer@logistiflow.local" };

        var start = () => McpTestServer.StartAsync(new FakeGateway(), settings);

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain(GatewayOptions.AccessTokenVariable).And.Contain(GatewayOptions.PasswordVariable);
    }

    [Fact]
    public void Bind_DefaultsToTheLocalGateway_AndKeepsPathsUnderTheBaseUrl()
    {
        var defaults = new GatewayOptions();
        GatewayOptions.Bind(new ConfigurationBuilder().Build(), defaults);

        var custom = new GatewayOptions();
        GatewayOptions.Bind(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { [GatewayOptions.BaseUrlVariable] = "https://logistiflow.example/edge" }).Build(),
            custom);

        defaults.BaseUrl.Should().Be(GatewayOptions.DefaultBaseUrl);
        new Uri(custom.BaseUrl, "api/orders").AbsoluteUri.Should().Be("https://logistiflow.example/edge/api/orders");
    }

    [Theory]
    [InlineData("token", null, null, true)]
    [InlineData(null, "viewer@logistiflow.local", "secret", true)]
    [InlineData(null, "viewer@logistiflow.local", null, false)]
    [InlineData(" ", null, null, false)]
    public void HasCredentials_NeedsATokenOrBothEmailAndPassword(string? token, string? email, string? password, bool expected) =>
        new GatewayOptions { AccessToken = token, Email = email, Password = password }.HasCredentials.Should().Be(expected);
}
