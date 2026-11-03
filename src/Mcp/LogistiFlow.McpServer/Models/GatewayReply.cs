using LogistiFlow.McpServer.Enums;

namespace LogistiFlow.McpServer.Models;

/// <summary>Factories for <see cref="GatewayReply{T}"/>.</summary>
public static class GatewayReply
{
    public static GatewayReply<T> Ok<T>(T data) => new(GatewayOutcome.Ok, null, data);

    public static GatewayReply<T> Fail<T>(GatewayOutcome outcome, string message) => new(outcome, message, default);
}

/// <summary>The result of one gateway call: data, or the outcome and a message safe to show the client.</summary>
public sealed record GatewayReply<T>(GatewayOutcome Outcome, string? Message, T? Data)
{
    public bool IsOk => Outcome == GatewayOutcome.Ok;

    public GatewayReply<TOther> As<TOther>() => new(Outcome, Message, default);
}
