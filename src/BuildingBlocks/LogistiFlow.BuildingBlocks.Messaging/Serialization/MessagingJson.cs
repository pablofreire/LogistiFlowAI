using System.Text.Json;

namespace LogistiFlow.BuildingBlocks.Messaging.Serialization;

public static class MessagingJson
{
    /// <summary>camelCase, case-insensitive reads: tolerant to additive contract changes.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
