using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Gateway.Domain.Errors;
using LogistiFlow.Gateway.Interfaces;
using LogistiFlow.Gateway.UseCases.Auth.Login.Contracts;
using LogistiFlow.Gateway.UseCases.Auth.Login.Interfaces;

namespace LogistiFlow.Gateway.UseCases.Auth.Login.Implementations;

/// <summary>
/// Verifies credentials and issues an access token. An unknown e-mail still pays for one BCrypt verification,
/// so response time does not reveal whether an account exists.
/// </summary>
public sealed class LoginUseCase(
    IValidator<LoginRequest> validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer) : ILoginUseCase
{
    // Hash of a random value computed once per process; only used to equalise timing for unknown accounts.
    private static readonly Lazy<string> TimingEqualizerHash = new(() => BCrypt.Net.BCrypt.EnhancedHashPassword(Guid.NewGuid().ToString("N"), 11));

    public async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        var user = await users.GetActiveByEmailAsync(request.Email.Trim(), cancellationToken);
        var passwordMatches = SafeVerify(request.Password, user?.PasswordHash ?? TimingEqualizerHash.Value);
        if (user is null || !passwordMatches)
        {
            return AuthErrors.InvalidCredentials;
        }

        var token = tokenIssuer.Issue(user);
        return new LoginResponse(token.Value, token.ExpiresAt, new AuthenticatedUser(user.PublicId, user.Email, user.DisplayName, user.Role));
    }

    private bool SafeVerify(string password, string hash)
    {
        try
        {
            return passwordHasher.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
