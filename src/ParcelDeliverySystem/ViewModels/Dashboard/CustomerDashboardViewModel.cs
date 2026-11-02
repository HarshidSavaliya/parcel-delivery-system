using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.ViewModels.Dashboard;

/// <summary>
/// Aggregated view model for the Customer portal dashboard.
/// </summary>
public class CustomerDashboardViewModel
{
    public User Customer { get; set; } = new();
    public int ActiveBookingsCount { get; set; }
    public int DeliveredParcelsCount { get; set; }
    public int SavedAddressesCount { get; set; }
    public int UnreadNotificationsCount { get; set; }

    public IReadOnlyList<ParcelDeliverySystem.Models.Entities.Parcel> RecentParcels { get; set; } = new List<ParcelDeliverySystem.Models.Entities.Parcel>();
    public IReadOnlyList<Address> SavedAddresses { get; set; } = new List<Address>();
    public IReadOnlyList<Notification> RecentNotifications { get; set; } = new List<Notification>();
}
