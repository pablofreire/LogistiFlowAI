namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Contracts;

public sealed record ResolveShipmentExceptionRequest(Guid ExceptionId, string Resolution, string ResolvedBy);
