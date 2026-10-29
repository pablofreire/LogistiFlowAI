using FluentValidation;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Contracts;
using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Validators;

public sealed class PerformFulfillmentJobActionRequestValidator : AbstractValidator<PerformFulfillmentJobActionRequest>
{
    public PerformFulfillmentJobActionRequestValidator()
    {
        RuleFor(request => request.JobId).NotEmpty();
        RuleFor(request => request.Action).IsInEnum();
        RuleFor(request => request.PerformedBy).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Reason).MaximumLength(500);
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MinimumLength(3)
            .When(request => request.Action == FulfillmentJobAction.ReportIssue)
            .WithMessage("Describe the issue (at least 3 characters).");
    }
}
