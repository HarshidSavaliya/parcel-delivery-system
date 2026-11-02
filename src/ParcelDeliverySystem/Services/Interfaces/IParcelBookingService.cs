using ParcelDeliverySystem.Models.DTOs.Pricing;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.ViewModels.Parcels;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Business service contract for parcel booking workflow, quotes, authorization rules, and lifecycle management.
/// </summary>
public interface IParcelBookingService
{
    Task<Parcel> BookParcelAsync(BookParcelViewModel model, string customerId);
    Task<DeliveryChargeBreakdownDto> CalculateQuoteAsync(BookingQuoteRequestDto request);
    Task<Parcel?> GetParcelDetailsAsync(string trackingId, string? requestedByUserId = null, bool isAdmin = false);
    Task<IReadOnlyList<Parcel>> GetCustomerParcelsAsync(string customerId);
    Task<bool> CancelBookingAsync(string trackingId, string customerId);
}
