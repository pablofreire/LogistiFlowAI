using FluentValidation;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;
using LogistiFlow.AiOperations.Domain.Entities;

namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Validators;

public sealed class AskCopilotRequestValidator : AbstractValidator<AskCopilotRequest>
{
    public AskCopilotRequestValidator()
    {
        RuleFor(request => request.Question).NotEmpty().MaximumLength(CopilotInteractionEntity.MaxQuestionLength);
        RuleFor(request => request.ConversationId).NotEqual(Guid.Empty).When(request => request.ConversationId is not null);
        RuleFor(request => request.UserEmail).NotEmpty().MaximumLength(254);
        RuleFor(request => request.UserRole).NotEmpty().MaximumLength(40);
    }
}
