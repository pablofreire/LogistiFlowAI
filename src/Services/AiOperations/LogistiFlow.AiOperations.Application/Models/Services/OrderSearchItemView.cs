namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>An order row of the Orders search.</summary>
public sealed record OrderSearchItemView(Guid Id, string OrderNumber, string Status);
