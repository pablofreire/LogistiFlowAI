using LogistiFlow.BuildingBlocks.Domain.Entities;
using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Domain.Entities;

/// <summary>An operational problem on a shipment, opened automatically and resolved automatically or by an operator.</summary>
public sealed class ShipmentExceptionEntity : Entity
{
    private ShipmentExceptionEntity(ShipmentExceptionType type, ShipmentExceptionStatus status, string details)
    {
        Type = type;
        Status = status;
        Details = details;
    }

    public ShipmentExceptionType Type { get; }

    public ShipmentExceptionStatus Status { get; private set; }

    public string Details { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? Resolution { get; private set; }

    public string? ResolvedBy { get; private set; }

    /// <summary>Set when a stored exception changed and must be written back.</summary>
    public bool IsModified { get; private set; }

    public bool IsOpen => Status == ShipmentExceptionStatus.Open;

    public static ShipmentExceptionEntity Open(ShipmentExceptionType type, string details, DateTimeOffset now)
    {
        var exception = new ShipmentExceptionEntity(type, ShipmentExceptionStatus.Open, Truncate(details));
        exception.Initialize(now);
        return exception;
    }

    public static ShipmentExceptionEntity Restore(
        long id,
        Guid publicId,
        ShipmentExceptionType type,
        ShipmentExceptionStatus status,
        string details,
        DateTimeOffset? resolvedAt,
        string? resolution,
        string? resolvedBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var exception = new ShipmentExceptionEntity(type, status, details)
        {
            ResolvedAt = resolvedAt,
            Resolution = resolution,
            ResolvedBy = resolvedBy,
        };
        exception.Restore(id, publicId, isActive: true, createdAt, updatedAt);
        return exception;
    }

    public void UpdateDetails(string details, DateTimeOffset now)
    {
        Details = Truncate(details);
        Touch(now);
        IsModified = true;
    }

    public void Resolve(string resolution, string resolvedBy, DateTimeOffset now)
    {
        Status = ShipmentExceptionStatus.Resolved;
        Resolution = Truncate(resolution);
        ResolvedBy = resolvedBy;
        ResolvedAt = now;
        Touch(now);
        IsModified = true;
    }

    public void MarkPersisted() => IsModified = false;

    private static string Truncate(string text) => text.Length <= 500 ? text : text[..500];
}
