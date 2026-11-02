using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Agent;

/// <summary>
/// View model for agent managing an active parcel delivery with real-time status actions and navigation map.
/// </summary>
public class AgentDeliveryManagementViewModel
{
    public Parcel Parcel { get; set; } = new();
    public AgentAssignment? CurrentAssignment { get; set; }

    public bool CanMarkPickedUp => Parcel.Status == ParcelStatus.AgentAssigned;
    public bool CanMarkInTransit => Parcel.Status == ParcelStatus.PickedUp;
    public bool CanMarkOutForDelivery => Parcel.Status == ParcelStatus.InTransit;
    public bool CanMarkDelivered => Parcel.Status == ParcelStatus.OutForDelivery || Parcel.Status == ParcelStatus.InTransit;
    public bool CanMarkFailed => Parcel.Status == ParcelStatus.AgentAssigned || 
                                 Parcel.Status == ParcelStatus.PickedUp || 
                                 Parcel.Status == ParcelStatus.InTransit || 
                                 Parcel.Status == ParcelStatus.OutForDelivery;

    public bool IsCompleted => Parcel.Status == ParcelStatus.Delivered;
    public bool IsFailed => Parcel.Status == ParcelStatus.DeliveryFailed || Parcel.Status == ParcelStatus.FailedAttempt;
    public bool IsCancelled => Parcel.Status == ParcelStatus.Cancelled;
}
