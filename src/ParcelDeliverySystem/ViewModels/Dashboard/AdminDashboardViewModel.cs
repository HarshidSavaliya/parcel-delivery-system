using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.ViewModels.Dashboard;

/// <summary>
/// Aggregated view model for Central System Administrator dashboard.
/// </summary>
public class AdminDashboardViewModel
{
    public long TotalUsersCount { get; set; }
    public long CustomersCount { get; set; }
    public long AgentsCount { get; set; }
    public long AvailableAgentsCount { get; set; }

    public long TotalParcelsCount { get; set; }
    public long PendingPickupCount { get; set; }
    public long InTransitCount { get; set; }
    public long DeliveredCount { get; set; }
    public long CancelledCount { get; set; }
    public long FailedDeliveriesCount { get; set; }

    public IReadOnlyList<UserListItemViewModel> RecentUsers { get; set; } = new List<UserListItemViewModel>();
    public IReadOnlyList<ParcelDeliverySystem.Models.Entities.Parcel> RecentParcels { get; set; } = new List<ParcelDeliverySystem.Models.Entities.Parcel>();
    public IReadOnlyList<UserListItemViewModel> AvailableAgentsList { get; set; } = new List<UserListItemViewModel>();
}
