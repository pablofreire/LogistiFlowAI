namespace LogistiFlow.BuildingBlocks.Web.Api;

/// <summary>
/// Factory methods for <see cref="ApiResponse{T}"/>.
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string? traceId) => new(true, data, null, traceId);

    public static ApiResponse<object> Fail(ApiError error, string? traceId) => new(false, null, error, traceId);
}
