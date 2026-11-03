using FluentValidation;
using LogistiFlow.AiOperations.Application.Tools;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Implementations;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Interfaces;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Implementations;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Interfaces;
using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Implementations;
using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogistiFlow.AiOperations.Application.DependencyInjection;

public static class AiOperationsApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAiOperationsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AskCopilotUseCase>(ServiceLifetime.Singleton);
        services.TryAddSingleton(TimeProvider.System);

        // One log and one toolset per question (request scope), so tool calls of concurrent questions never mix.
        services.AddScoped<CopilotToolCallLog>();
        services.AddScoped<OperationsToolset>();

        services.AddScoped<IAskCopilotUseCase, AskCopilotUseCase>();
        services.AddScoped<IGetCopilotStatusUseCase, GetCopilotStatusUseCase>();
        services.AddScoped<IListCopilotInteractionsUseCase, ListCopilotInteractionsUseCase>();
        return services;
    }
}
