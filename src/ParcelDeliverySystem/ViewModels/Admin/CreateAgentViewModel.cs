using System.ComponentModel.DataAnnotations;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Admin;

/// <summary>
/// View model for administrator creating a new delivery partner agent account.
/// </summary>
public class CreateAgentViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Please provide a valid contact number.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Temporary password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Registered Base / Depot Address")]
    public string? Address { get; set; }

    [Display(Name = "Vehicle Type")]
    public string? VehicleType { get; set; } = "Motorcycle / Scooter";

    [Display(Name = "License Plate Number")]
    public string? VehiclePlateNumber { get; set; }

    [Display(Name = "Driving License Number")]
    public string? DrivingLicenseNumber { get; set; }

    [Display(Name = "Initial Availability Status")]
    public AgentAvailabilityStatus AvailabilityStatus { get; set; } = AgentAvailabilityStatus.Available;
}
