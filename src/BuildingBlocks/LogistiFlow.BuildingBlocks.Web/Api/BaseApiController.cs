using System.Diagnostics;
using LogistiFlow.BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.BuildingBlocks.Web.Api;

/// <summary>
/// Base class for REST controllers. Controllers stay thin: they bind input, call one use case
/// and translate its <see cref="Result{TValue}"/> into an <see cref="ApiResponse{T}"/>.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected async Task<IActionResult> ExecuteAsync<T>(Func<Task<Result<T>>> operation, int successStatusCode = StatusCodes.Status200OK)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var result = await operation();
        return result.IsSuccess
            ? StatusCode(successStatusCode, ApiResponse.Ok(result.Value, TraceId))
            : Problem(result.Error);
    }

    protected async Task<IActionResult> ExecuteAsync(Func<Task<Result>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var result = await operation();
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    protected string TraceId => Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;

    /// <summary>Wraps <paramref name="data"/> in the success envelope with an explicit status code.</summary>
    protected ObjectResult Envelope<T>(T data, int statusCode) => StatusCode(statusCode, ApiResponse.Ok(data, TraceId));

    /// <summary>Translates an expected failure into the error envelope and its HTTP status code.</summary>
    protected ObjectResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return StatusCode(statusCode, ApiResponse.Fail(new ApiError(error.Code, error.Message, error.Details), TraceId));
    }
}
