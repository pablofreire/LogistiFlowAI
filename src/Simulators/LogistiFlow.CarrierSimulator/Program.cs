using LogistiFlow.CarrierSimulator.Booking;
using LogistiFlow.CarrierSimulator.Endpoints;
using LogistiFlow.CarrierSimulator.Options;

// Container health check without curl in the image: `dotnet LogistiFlow.CarrierSimulator.dll --health-probe`.
if (args.Contains("--health-probe", StringComparer.Ordinal))
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    try
    {
        using var response = await client.GetAsync(new Uri("http://127.0.0.1:8080/health"));
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
    {
        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<SimulatorOptions>().Bind(builder.Configuration.GetSection(SimulatorOptions.SectionName));
builder.Services.AddSingleton<BookingStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapCarrierEndpoints();

await app.RunAsync();
return 0;
