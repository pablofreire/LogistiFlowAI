namespace LogistiFlow.BuildingBlocks.Messaging.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";

    /// <summary>Durable topic exchange every service publishes its integration events to.</summary>
    public string Exchange { get; set; } = "logistiflow.events";
}
