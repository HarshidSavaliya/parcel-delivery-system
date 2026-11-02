using System.ComponentModel.DataAnnotations;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Admin;

/// <summary>
/// View model for administrator editing an existing delivery agent's account.
/// </summary>
public class EditAgentViewModel
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

    [Display(Name = "Vehicle Type")]
    public string? VehicleType { get; set; }

    [Display(Name = "License Plate Number")]
    public string? VehiclePlateNumber { get; set; }

    [Display(Name = "Driving License Number")]
    public string? DrivingLicenseNumber { get; set; }

    [Display(Name = "Availability Status")]
    public AgentAvailabilityStatus AvailabilityStatus { get; set; }

    [Display(Name = "Account Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Reset Password (Leave blank to keep current)")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}
