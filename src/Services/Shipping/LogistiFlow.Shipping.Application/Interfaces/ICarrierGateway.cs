using LogistiFlow.Shipping.Application.Models;

namespace LogistiFlow.Shipping.Application.Interfaces;

/// <summary>The carrier API. Implementations never throw for carrier-side failures; they return a failed result.</summary>
public interface ICarrierGateway
{
    Task<CarrierBookingResult> BookAsync(CarrierBookingRequest request, CancellationToken cancellationToken);

    Task<CarrierTrackingResult> GetTrackingAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken);
}
