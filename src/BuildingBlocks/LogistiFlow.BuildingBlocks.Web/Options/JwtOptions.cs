namespace LogistiFlow.BuildingBlocks.Web.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "logistiflow-gateway";

    public string Audience { get; set; } = "logistiflow-api";

    /// <summary>HMAC-SHA256 key shared by the issuer and the validating services. At least 32 bytes; never committed.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);
}
