using System.ComponentModel.DataAnnotations;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Users;

/// <summary>
/// View model for displaying and editing user profile information.
/// </summary>
public class UserProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Delivery Agent Specific
    [Display(Name = "Availability Status")]
    public AgentAvailabilityStatus AvailabilityStatus { get; set; }

    [Display(Name = "Vehicle Type")]
    public string? VehicleType { get; set; }

    [Display(Name = "Vehicle Plate Number")]
    public string? VehiclePlateNumber { get; set; }

    [Display(Name = "Driving License Number")]
    public string? DrivingLicenseNumber { get; set; }

    [Display(Name = "Rating")]
    public double Rating { get; set; }

    [Display(Name = "Completed Deliveries")]
    public int CompletedDeliveriesCount { get; set; }
}
