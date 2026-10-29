using FluentValidation;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Products.Create.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Sku)
            .NotEmpty()
            .MaximumLength(40)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU may contain only letters, digits and hyphens.");
        RuleFor(request => request.Name).NotEmpty().Length(2, 150);
        RuleFor(request => request.Description).MaximumLength(1000);
    }
}
