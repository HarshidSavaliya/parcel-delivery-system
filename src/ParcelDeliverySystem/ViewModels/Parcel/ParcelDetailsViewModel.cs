using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.ViewModels.Parcels;

/// <summary>
/// Aggregated view model for detailed parcel tracking, receipt generation, and map display.
/// </summary>
public class ParcelDetailsViewModel
{
    public Models.Entities.Parcel Parcel { get; set; } = new();
    public User? Customer { get; set; }
    public User? AssignedAgent { get; set; }
    public bool CanCancel { get; set; }
    public bool IsOwnerOrAdmin { get; set; }
}
