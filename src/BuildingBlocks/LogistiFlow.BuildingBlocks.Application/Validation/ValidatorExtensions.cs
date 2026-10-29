using LogistiFlow.BuildingBlocks.Domain.Results;
using FluentValidation;

namespace LogistiFlow.BuildingBlocks.Application.Validation;

public static class ValidatorExtensions
{
    /// <summary>
    /// Validates <paramref name="request"/> and converts failures into a validation <see cref="Error"/>.
    /// Returns <see cref="Error.None"/> when the request is valid.
    /// </summary>
    public static async Task<Error> ValidateToErrorAsync<T>(
        this IValidator<T> validator,
        T request,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return Error.None;
        }

        var details = result.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray(), StringComparer.Ordinal);

        return Error.Validation("request.invalid", "One or more fields are invalid.", details);
    }
}
