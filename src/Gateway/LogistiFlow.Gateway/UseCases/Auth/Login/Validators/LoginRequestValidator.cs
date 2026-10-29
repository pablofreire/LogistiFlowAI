using FluentValidation;
using LogistiFlow.Gateway.UseCases.Auth.Login.Contracts;

namespace LogistiFlow.Gateway.UseCases.Auth.Login.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
    }
}
