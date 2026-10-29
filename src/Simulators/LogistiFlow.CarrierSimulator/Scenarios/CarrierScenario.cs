namespace LogistiFlow.CarrierSimulator.Scenarios;

public enum CarrierScenario
{
    Normal = 0,
    Delay = 1,
    TemporaryFailure = 2,
    Timeout = 3,
    Redelivery = 4,
    AddressNotFound = 5,
}
