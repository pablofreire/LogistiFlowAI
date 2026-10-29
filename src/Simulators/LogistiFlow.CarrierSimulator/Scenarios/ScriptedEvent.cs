namespace LogistiFlow.CarrierSimulator.Scenarios;

/// <param name="Step">Steps after booking at which the event becomes visible.</param>
/// <param name="Code">Carrier event code, for example <c>OUT_FOR_DELIVERY</c>.</param>
/// <param name="Description">Human-readable text, as carriers send it.</param>
/// <param name="Location">Where the event happened.</param>
/// <param name="EstimateShiftDays">Business days added to the delivery estimate from this event on.</param>
public sealed record ScriptedEvent(int Step, string Code, string Description, string Location, int EstimateShiftDays = 0);
