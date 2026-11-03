namespace LogistiFlow.AiOperations.Infrastructure.Services.Contracts;

/// <summary>The <c>ApiResponse</c> envelope every service answers with.</summary>
internal sealed record ServiceEnvelope<T>(T? Data, ServiceError? Error);
