namespace LogistiFlow.BuildingBlocks.Application.Paging.Models;

public sealed record PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public int Offset => (Page - 1) * PageSize;

    /// <summary>Clamps user input to a valid page window instead of rejecting it.</summary>
    public static PageRequest Normalize(int? page, int? pageSize) =>
        new(Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}
