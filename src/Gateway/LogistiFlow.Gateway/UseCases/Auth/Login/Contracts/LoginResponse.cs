namespace LogistiFlow.Gateway.UseCases.Auth.Login.Contracts;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthenticatedUser User);
