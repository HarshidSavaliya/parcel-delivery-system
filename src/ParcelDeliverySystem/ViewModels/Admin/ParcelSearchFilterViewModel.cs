using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Admin;

/// <summary>
/// View model for administrator multi-criteria server-side parcel searching and filtering.
/// </summary>
public class ParcelSearchFilterViewModel
{
    public string? SearchTerm { get; set; } // Tracking ID, Recipient Name, Phone, City
    public ParcelStatus? Status { get; set; }
    public DeliveryType? DeliveryType { get; set; }
    public string? AgentId { get; set; }
    public string? CustomerId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public IReadOnlyList<Parcel> Parcels { get; set; } = new List<Parcel>();
    public IReadOnlyList<User> AgentsList { get; set; } = new List<User>();
    public int TotalResultsCount => Parcels.Count;
}
