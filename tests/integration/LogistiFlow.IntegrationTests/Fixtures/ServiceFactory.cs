using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LogistiFlow.IntegrationTests.Fixtures;

/// <summary>
/// Hosts one real service in-process against the test containers. Settings are passed with
/// <c>UseSetting</c> so they are visible while <c>Program</c> registers services.
/// </summary>
public sealed class ServiceFactory<TMarker>(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<TMarker>
    where TMarker : class
{
    public HttpClient CreateClientAs(string? role)
    {
        var client = CreateClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.For(role));
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }
    }
}
