using System.Text.RegularExpressions;
using ModelContextProtocol;

namespace LogistiFlow.McpServer.Validators;

/// <summary>
/// Argument checks that run before any gateway call. A bad argument becomes an <see cref="McpException"/>, which the
/// SDK returns as a tool error the model can read and correct.
/// </summary>
public static partial class ToolArguments
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    public static readonly IReadOnlyList<string> ExceptionStatuses = ["Open", "Resolved"];

    public static readonly IReadOnlyList<string> ExceptionTypes = ["CarrierUnavailable", "Delay", "DeliveryAttemptFailed", "ReturnedToSender"];

    public static string OrderNumber(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return OrderNumberPattern().IsMatch(trimmed)
            ? trimmed
            : throw new McpException($"orderNumber must look like ORD-1025; got '{Shorten(value)}'.");
    }

    public static string Sku(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return SkuPattern().IsMatch(trimmed)
            ? trimmed
            : throw new McpException($"sku must be 1 to 40 letters, digits or dashes, like SKU-1003; got '{Shorten(value)}'.");
    }

    public static string? OneOf(string? value, IReadOnlyList<string> allowed, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return allowed.FirstOrDefault(option => string.Equals(option, value.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? throw new McpException($"{name} must be one of {string.Join(", ", allowed)}; got '{Shorten(value)}'.");
    }

    public static int Limit(int? value) =>
        value switch
        {
            null => DefaultLimit,
            >= 1 and <= MaxLimit => value.Value,
            _ => throw new McpException($"limit must be between 1 and {MaxLimit}; got {value}."),
        };

    // Echoes at most 40 characters of a rejected value, so a pasted document never flows back into the conversation.
    private static string Shorten(string? value) => value is null ? string.Empty : value.Length <= 40 ? value : value[..40] + "...";

    [GeneratedRegex("^ORD-[0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex OrderNumberPattern();

    [GeneratedRegex("^[A-Za-z0-9-]{1,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex SkuPattern();
}
