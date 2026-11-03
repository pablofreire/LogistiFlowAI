using System.ComponentModel.DataAnnotations;

namespace LogistiFlow.AiOperations.Infrastructure.Options;

/// <summary>Where the tools read from: each service directly, not through the gateway.</summary>
public sealed class OperationsServicesOptions
{
    public const string SectionName = "Services";

    [Required]
    public Uri Orders { get; set; } = null!;

    [Required]
    public Uri Inventory { get; set; } = null!;

    [Required]
    public Uri Fulfillment { get; set; } = null!;

    [Required]
    public Uri Shipping { get; set; } = null!;
}
