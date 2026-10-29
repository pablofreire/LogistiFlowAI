namespace LogistiFlow.Gateway.Options;

public sealed class LoginRateLimitOptions
{
    public const string SectionName = "RateLimiting:Login";

    public const string PolicyName = "login";

    /// <summary>Login attempts allowed per client IP within <see cref="Window"/>.</summary>
    public int PermitLimit { get; set; } = 10;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
