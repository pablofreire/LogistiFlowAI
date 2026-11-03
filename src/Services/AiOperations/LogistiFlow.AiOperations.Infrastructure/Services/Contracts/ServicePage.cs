namespace LogistiFlow.AiOperations.Infrastructure.Services.Contracts;

/// <summary>A <c>PagedResult</c> as the services serialise it.</summary>
internal sealed record ServicePage<T>(IReadOnlyList<T> Items, long TotalCount);
