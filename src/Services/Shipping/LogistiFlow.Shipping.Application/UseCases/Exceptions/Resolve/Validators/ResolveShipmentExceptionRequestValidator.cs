using FluentValidation;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Contracts;

namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Validators;

public sealed class ResolveShipmentExceptionRequestValidator : AbstractValidator<ResolveShipmentExceptionRequest>
{
    public ResolveShipmentExceptionRequestValidator()
    {
        RuleFor(request => request.ExceptionId).NotEmpty();
        RuleFor(request => request.Resolution).NotEmpty().Length(3, 500);
        RuleFor(request => request.ResolvedBy).NotEmpty().MaximumLength(120);
    }
}
