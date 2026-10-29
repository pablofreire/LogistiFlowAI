namespace LogistiFlow.Gateway.UseCases.Auth.Login.Contracts;

public sealed record AuthenticatedUser(Guid Id, string Email, string DisplayName, string Role);
