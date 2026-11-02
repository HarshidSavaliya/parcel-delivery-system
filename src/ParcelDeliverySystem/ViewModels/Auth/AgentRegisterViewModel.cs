using System.ComponentModel.DataAnnotations;
using ParcelDeliverySystem.Common.Attributes;

namespace ParcelDeliverySystem.ViewModels.Auth;

/// <summary>
/// Data Transfer & View model for Delivery Agent registration and onboarding.
/// </summary>
public class AgentRegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle type is required.")]
    [Display(Name = "Vehicle Type")]
    public string VehicleType { get; set; } = "Van / Courier Van"; // Motorcycle, Car, Van, Bicycle, Truck

    [Required(ErrorMessage = "Vehicle plate or registration number is required.")]
    [Display(Name = "Vehicle Plate Number")]
    public string VehiclePlateNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Driving license number is required.")]
    [Display(Name = "Driving License Number")]
    public string DrivingLicenseNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MustBeTrue(ErrorMessage = "You must agree to the Agent Partner Terms & Code of Conduct.")]
    [Display(Name = "I agree to the Agent Partner Terms & Code of Conduct")]
    public bool AgreeToTerms { get; set; }
}

