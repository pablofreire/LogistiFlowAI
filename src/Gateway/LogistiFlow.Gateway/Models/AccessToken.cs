namespace LogistiFlow.Gateway.Models;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
