using LogistiFlow.BuildingBlocks.Domain.Results;

namespace LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;

/// <summary>
/// A single application operation. Each use case has one request type, one response type and one class.
/// </summary>
public interface IUseCase<in TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}
