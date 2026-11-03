using System.Diagnostics;
using FluentValidation;
using LogistiFlow.AiOperations.Application.Agent;
using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Options;
using LogistiFlow.AiOperations.Application.Tools;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Interfaces;
using LogistiFlow.AiOperations.Domain.Entities;
using LogistiFlow.AiOperations.Domain.Errors;
using LogistiFlow.AiOperations.Domain.Interfaces.Repositories;
using LogistiFlow.AiOperations.Domain.Models;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Implementations;

/// <summary>
/// Answers one question with a <see cref="ChatClientAgent"/> over the configured provider and the read-only tools,
/// bounded by a timeout and a maximum number of tool rounds, then audits it: answered or failed, with every tool
/// call and the token usage. Earlier turns of the conversation are replayed as context.
/// </summary>
public sealed partial class AskCopilotUseCase(
    IValidator<AskCopilotRequest> validator,
    ICopilotChatClientFactory chatClients,
    OperationsToolset toolset,
    CopilotToolCallLog toolCalls,
    ICopilotInteractionRepository interactions,
    IUnitOfWork unitOfWork,
    IOptions<CopilotOptions> options,
    TimeProvider timeProvider,
    ILogger<AskCopilotUseCase> logger) : IAskCopilotUseCase
{
    public async Task<Result<AskCopilotResponse>> ExecuteAsync(AskCopilotRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        var settings = options.Value;
        var started = Stopwatch.GetTimestamp();
        var conversationId = request.ConversationId ?? Guid.CreateVersion7(timeProvider.GetUtcNow());
        var history = request.ConversationId is { } existing
            ? await interactions.GetRecentTurnsAsync(existing, request.UserEmail, settings.HistoryTurns, cancellationToken)
            : [];

        var provider = chatClients.Provider;
        var (response, failure) = await RunAgentAsync(Messages(history, request.Question), settings, cancellationToken);
        var usage = UsageOf(response?.Usage, settings);
        var durationMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var answer = response?.Text.Trim();
        if (failure is null && string.IsNullOrEmpty(answer))
        {
            failure = AiOperationsErrors.ProviderUnavailable;
        }

        var now = timeProvider.GetUtcNow();
        var interaction = failure is null
            ? CopilotInteractionEntity.Answered(conversationId, request.UserEmail, request.UserRole, request.Question, answer!, provider, usage, durationMs, toolCalls.Calls, now)
            : CopilotInteractionEntity.Failed(conversationId, request.UserEmail, request.UserRole, request.Question, failure.Message, provider, usage, durationMs, toolCalls.Calls, now);

        await unitOfWork.ExecuteAsync(token => interactions.AddAsync(interaction, token), cancellationToken);

        if (failure is not null)
        {
            return failure;
        }

        return new AskCopilotResponse(
            interaction.PublicId,
            interaction.ConversationId,
            interaction.Answer,
            provider.Name,
            provider.Model,
            provider.Simulated,
            [.. interaction.ToolCalls.Select(call => new CopilotToolCallItem(call.Sequence, call.ToolName, call.ArgumentsJson, ToolResult.StatusOf(call.Outcome), call.Summary, call.DurationMs))],
            new CopilotUsageItem(usage.InputTokens, usage.OutputTokens, usage.TotalTokens, usage.EstimatedCostUsd),
            durationMs,
            now);
    }

    private static List<ChatMessage> Messages(IReadOnlyList<CopilotTurn> history, string question)
    {
        var messages = new List<ChatMessage>(history.Count * 2 + 1);
        foreach (var turn in history)
        {
            messages.Add(new ChatMessage(ChatRole.User, turn.Question));
            messages.Add(new ChatMessage(ChatRole.Assistant, turn.Answer));
        }

        messages.Add(new ChatMessage(ChatRole.User, question.Trim()));
        return messages;
    }

    private static CopilotUsage UsageOf(UsageDetails? usage, CopilotOptions settings)
    {
        if (usage is null)
        {
            return CopilotUsage.None;
        }

        var cost = ((usage.InputTokenCount ?? 0) * settings.InputCostPerThousandTokensUsd / 1000m)
                   + ((usage.OutputTokenCount ?? 0) * settings.OutputCostPerThousandTokensUsd / 1000m);
        return new CopilotUsage(usage.InputTokenCount, usage.OutputTokenCount, usage.TotalTokenCount, Math.Round(cost, 6));
    }

    private async Task<(AgentResponse? Response, Error? Failure)> RunAgentAsync(List<ChatMessage> messages, CopilotOptions settings, CancellationToken cancellationToken)
    {
        using var client = chatClients.Create()
            .AsBuilder()
            .UseFunctionInvocation(configure: invoker => invoker.MaximumIterationsPerRequest = settings.MaxToolRounds)
            .Build();

        var agent = new ChatClientAgent(client, new ChatClientAgentOptions
        {
            Name = CopilotPrompts.AgentName,
            UseProvidedChatClientAsIs = true,
            ChatOptions = new ChatOptions
            {
                Instructions = CopilotPrompts.Instructions,
                Tools = toolset.CreateTools(),
                MaxOutputTokens = settings.MaxOutputTokens,
            },
        });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Timeout);
        try
        {
            return (await agent.RunAsync(messages, cancellationToken: timeout.Token), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogTimedOut(logger, settings.Timeout);
            return (null, AiOperationsErrors.QuestionTimedOut);
        }
#pragma warning disable CA1031 // Any provider failure becomes an audited 503; the logistics platform is unaffected.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogProviderFailed(logger, exception, chatClients.Provider.Name);
            return (null, AiOperationsErrors.ProviderUnavailable);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Copilot question timed out after {Timeout}")]
    private static partial void LogTimedOut(ILogger logger, TimeSpan timeout);

    [LoggerMessage(Level = LogLevel.Error, Message = "Copilot provider {Provider} failed")]
    private static partial void LogProviderFailed(ILogger logger, Exception exception, string provider);
}
