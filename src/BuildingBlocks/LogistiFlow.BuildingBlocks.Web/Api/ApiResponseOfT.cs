namespace LogistiFlow.BuildingBlocks.Web.Api;

/// <summary>
/// Envelope used by every REST endpoint so clients handle success and failure uniformly.
/// </summary>
public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error, string? TraceId);
