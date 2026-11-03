namespace LogistiFlow.AiOperations.Application.Models;

/// <summary>The first page of a service list and how many items the list holds in total.</summary>
public sealed record PageView<T>(IReadOnlyList<T> Items, long TotalCount)
{
    public bool IsTruncated => TotalCount > Items.Count;
}
