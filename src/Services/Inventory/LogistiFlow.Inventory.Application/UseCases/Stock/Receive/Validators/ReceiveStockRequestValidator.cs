using FluentValidation;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Validators;

public sealed class ReceiveStockRequestValidator : AbstractValidator<ReceiveStockRequest>
{
    public ReceiveStockRequestValidator()
    {
        RuleFor(request => request.WarehouseCode).NotEmpty().MaximumLength(20);
        RuleFor(request => request.Sku).NotEmpty().MaximumLength(40);
        RuleFor(request => request.Quantity).InclusiveBetween(1, 1_000_000);
        RuleFor(request => request.Reference).NotEmpty().MaximumLength(60);
    }
}
