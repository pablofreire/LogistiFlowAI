using FluentValidation;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Create.Validators;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public const int MaxLines = 50;
    public const int MaxQuantityPerLine = 10_000;

    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9._:-]+$").WithMessage("Idempotency-Key may contain only letters, digits and . _ : -");

        RuleFor(request => request.CustomerName).NotEmpty().Length(2, 120);
        RuleFor(request => request.DestinationCity).NotEmpty().Length(2, 80);
        RuleFor(request => request.DestinationCountry)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$").WithMessage("Destination country must be an ISO 3166-1 alpha-2 code.");

        RuleFor(request => request.Lines)
            .NotEmpty().WithMessage("An order needs at least one line.")
            .Must(lines => lines.Count <= MaxLines).WithMessage($"An order can have at most {MaxLines} lines.")
            .Must(lines => lines.Select(line => line.Sku.ToUpperInvariant()).Distinct(StringComparer.Ordinal).Count() == lines.Count)
            .WithMessage("Each SKU may appear only once per order.")
            .When(request => request.Lines is not null && request.Lines.All(line => line?.Sku is not null));

        RuleForEach(request => request.Lines).ChildRules(line =>
        {
            line.RuleFor(item => item.Sku)
                .NotEmpty()
                .MaximumLength(40)
                .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU may contain only letters, digits and hyphens.");
            line.RuleFor(item => item.Quantity).InclusiveBetween(1, MaxQuantityPerLine);
        });
    }
}
