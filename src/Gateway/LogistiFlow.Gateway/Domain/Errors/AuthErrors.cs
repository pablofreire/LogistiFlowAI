using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.Gateway.Domain.Errors;

public static class AuthErrors
{
    /// <summary>Same error for an unknown e-mail and a wrong password, so accounts cannot be enumerated.</summary>
    public static readonly Error InvalidCredentials = Error.Unauthorized("auth.invalid_credentials", "The e-mail or password is incorrect.");
}
