using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>An order with its status timeline.</summary>
public sealed record OrderDetailsData(OrderView Order, IReadOnlyList<StatusChangeView> History);
