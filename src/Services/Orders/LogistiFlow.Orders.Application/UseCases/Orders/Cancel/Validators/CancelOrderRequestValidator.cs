using FluentValidation;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Contracts;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Validators;

public sealed class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(request => request.OrderId).NotEmpty();
        RuleFor(request => request.Reason).NotEmpty().Length(3, 500);
    }
}
