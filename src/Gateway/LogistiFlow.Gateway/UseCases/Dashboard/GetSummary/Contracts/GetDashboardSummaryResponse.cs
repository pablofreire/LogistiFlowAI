using System.Text.Json;

namespace LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Contracts;

/// <summary>
/// Metrics of every service, fetched in parallel. A section is null when its service did not answer, and the
/// service is then listed in <c>UnavailableSources</c>; the dashboard shows partial data instead of an error.
/// </summary>
public sealed record GetDashboardSummaryResponse(
    JsonElement? Orders,
    JsonElement? Inventory,
    JsonElement? Fulfillment,
    JsonElement? Shipping,
    IReadOnlyList<string> UnavailableSources,
    DateTimeOffset GeneratedAt);
