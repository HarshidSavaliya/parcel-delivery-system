using System.ComponentModel.DataAnnotations;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Agent;

/// <summary>
/// View model for delivery agent profile display and editing.
/// </summary>
public class AgentProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Please provide a valid contact number.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Registered Base / Depot Address")]
    public string? Address { get; set; }

    [Display(Name = "Current Latitude")]
    public double? CurrentLatitude { get; set; }

    [Display(Name = "Current Longitude")]
    public double? CurrentLongitude { get; set; }

    [Display(Name = "Vehicle Type")]
    public string? VehicleType { get; set; }

    [Display(Name = "Vehicle Plate Number")]
    public string? VehiclePlateNumber { get; set; }

    [Display(Name = "Driving License Number")]
    public string? DrivingLicenseNumber { get; set; }

    [Display(Name = "Availability Status")]
    public AgentAvailabilityStatus AvailabilityStatus { get; set; } = AgentAvailabilityStatus.Offline;

    public int CompletedDeliveriesCount { get; set; }

    public double Rating { get; set; } = 5.0;
}
