using System.ComponentModel.DataAnnotations;

namespace ParcelDeliverySystem.ViewModels.Agent;

/// <summary>
/// View model for logging an unfulfilled or failed delivery attempt.
/// </summary>
public class ReportFailureViewModel
{
    [Required]
    public string ParcelId { get; set; } = string.Empty;

    public string TrackingId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a reason for delivery failure.")]
    [Display(Name = "Failure Reason")]
    public string Reason { get; set; } = "Customer unavailable"; // Customer unavailable, Incorrect address, Customer refused, Damaged parcel, Other

    [Required(ErrorMessage = "Please provide explanatory notes explaining the failure.")]
    [Display(Name = "Detailed Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string Notes { get; set; } = string.Empty;
}
