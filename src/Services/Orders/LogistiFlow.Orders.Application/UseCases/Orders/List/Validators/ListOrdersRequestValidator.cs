using FluentValidation;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Contracts;

namespace LogistiFlow.Orders.Application.UseCases.Orders.List.Validators;

public sealed class ListOrdersRequestValidator : AbstractValidator<ListOrdersRequest>
{
    public ListOrdersRequestValidator()
    {
        RuleFor(request => request.Search).MaximumLength(60);
        RuleFor(request => request.Status).IsInEnum().When(request => request.Status.HasValue);
    }
}
