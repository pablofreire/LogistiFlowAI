namespace LogistiFlow.BuildingBlocks.Web.Hosting;

/// <summary>
/// Container health check without shipping curl in the runtime image: <c>dotnet App.dll --health-probe</c> calls
/// the service's own readiness endpoint and exits 0 when it answers 200.
/// </summary>
public static class HealthProbe
{
    public const string Command = "--health-probe";

    public static bool IsRequested(string[] args) => args.Contains(Command, StringComparer.Ordinal);

    public static async Task<int> RunAsync()
    {
        var port = Environment.GetEnvironmentVariable("HEALTH_PROBE_PORT") ?? "8080";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await client.GetAsync(new Uri($"http://127.0.0.1:{port}/health/ready"));
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (HttpRequestException)
        {
            return 1;
        }
        catch (TaskCanceledException)
        {
            return 1;
        }
    }
}
