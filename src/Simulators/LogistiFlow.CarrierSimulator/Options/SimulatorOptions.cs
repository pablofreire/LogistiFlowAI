namespace LogistiFlow.CarrierSimulator.Options;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>Compressed time: one scripted tracking step becomes visible per interval.</summary>
    public TimeSpan StepInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>How long the timeout scenario holds its first booking response. Must exceed the client's timeout.</summary>
    public TimeSpan TimeoutDelay { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Number of booking calls the temporary-failure scenario answers with 503 before it succeeds.</summary>
    public int TemporaryFailureCount { get; set; } = 2;
}
