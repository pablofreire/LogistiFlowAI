namespace LogistiFlow.McpServer.Exceptions;

/// <summary>The gateway refused the configured e-mail and password. The message never contains the credentials.</summary>
public sealed class GatewaySignInException : Exception
{
    public GatewaySignInException()
    {
    }

    public GatewaySignInException(string message)
        : base(message)
    {
    }

    public GatewaySignInException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
