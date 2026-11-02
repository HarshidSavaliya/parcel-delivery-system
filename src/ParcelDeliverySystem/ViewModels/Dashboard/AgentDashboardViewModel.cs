using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Dashboard;

/// <summary>
/// Aggregated view model for Delivery Agent field operations dashboard.
/// </summary>
public class AgentDashboardViewModel
{
    public User Agent { get; set; } = new();
    public AgentAvailabilityStatus AvailabilityStatus { get; set; }
    public int ActiveDeliveriesCount { get; set; }
    public int CompletedTodayCount { get; set; }
    public int AvailablePoolCount { get; set; }
    public double Rating { get; set; }

    public IReadOnlyList<ParcelDeliverySystem.Models.Entities.Parcel> AssignedDeliveries { get; set; } = new List<ParcelDeliverySystem.Models.Entities.Parcel>();
    public IReadOnlyList<ParcelDeliverySystem.Models.Entities.Parcel> AvailableRequests { get; set; } = new List<ParcelDeliverySystem.Models.Entities.Parcel>();
    public IReadOnlyList<Notification> RecentNotifications { get; set; } = new List<Notification>();
}
