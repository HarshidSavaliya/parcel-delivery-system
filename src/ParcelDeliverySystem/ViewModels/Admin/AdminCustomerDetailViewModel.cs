using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.ViewModels.Admin;

/// <summary>
/// View model for administrator inspecting detailed customer profile, saved addresses, and shipment history.
/// Never exposes password hash.
/// </summary>
public class AdminCustomerDetailViewModel
{
    public User Customer { get; set; } = new();
    public IReadOnlyList<Address> SavedAddresses { get; set; } = new List<Address>();
    public IReadOnlyList<Parcel> BookingHistory { get; set; } = new List<Parcel>();
    public decimal TotalSpent { get; set; }
    public int CompletedBookingsCount { get; set; }
}
