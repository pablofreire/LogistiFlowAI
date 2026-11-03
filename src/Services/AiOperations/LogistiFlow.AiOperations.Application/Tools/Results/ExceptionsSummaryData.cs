using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>Exceptions opened in the last <c>SinceHours</c> hours.</summary>
public sealed record ExceptionsSummaryData(
    int SinceHours,
    int Total,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyList<ShipmentExceptionView> Items,
    bool Truncated);
