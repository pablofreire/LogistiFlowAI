namespace LogistiFlow.McpServer.Enums;

/// <summary>How a gateway call ended, so a tool can tell the client exactly why it has no data.</summary>
public enum GatewayOutcome
{
    Ok = 0,
    NotFound = 1,
    Forbidden = 2,
    Unauthorized = 3,
    Invalid = 4,
    Unavailable = 5,
}
