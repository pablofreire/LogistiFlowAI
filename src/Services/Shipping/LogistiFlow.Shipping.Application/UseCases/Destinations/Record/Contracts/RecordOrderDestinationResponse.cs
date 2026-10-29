namespace LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Contracts;

/// <summary><c>Recorded</c> is <c>false</c> when the event carried no destination (a message written before Phase 2).</summary>
public sealed record RecordOrderDestinationResponse(bool Recorded);
