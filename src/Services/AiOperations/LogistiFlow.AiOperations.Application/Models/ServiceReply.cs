using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Domain.Enums;

namespace LogistiFlow.AiOperations.Application.Models;

/// <summary>Builds <see cref="ServiceReply{T}"/> values.</summary>
public static class ServiceReply
{
    public static ServiceReply<T> Ok<T>(T data) => new(CopilotToolCallOutcome.Ok, data, null);

    public static ServiceReply<T> Fail<T>(CopilotToolCallOutcome outcome, string message) => new(outcome, default, message);
}

/// <summary>
/// What a service call returned, without throwing: the data, or why there is none (not found, forbidden for this
/// user, invalid input, service unavailable). Tools pass the reason on instead of inventing data.
/// </summary>
public sealed record ServiceReply<T>(CopilotToolCallOutcome Outcome, T? Data, string? Message) : IServiceReply
{
    public bool IsOk => Outcome == CopilotToolCallOutcome.Ok && Data is not null;

    /// <summary>Carries a failure of this call over to another result type.</summary>
    public ServiceReply<TOther> As<TOther>() => new(Outcome, default, Message);
}
