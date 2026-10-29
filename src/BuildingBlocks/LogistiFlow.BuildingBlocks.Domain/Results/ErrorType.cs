namespace LogistiFlow.BuildingBlocks.Domain.Results;

/// <summary>
/// Classifies an <see cref="Error"/> so the presentation layer can translate it into the right status code.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unauthorized = 5,
}
