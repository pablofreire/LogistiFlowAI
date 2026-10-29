namespace LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Contracts;

public sealed record GetDashboardSummaryRequest(string? Authorization, string CorrelationId);
