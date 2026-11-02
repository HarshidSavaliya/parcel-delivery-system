using System.ComponentModel.DataAnnotations;

namespace ParcelDeliverySystem.ViewModels.Agent;

/// <summary>
/// View model for capturing delivery completion confirmation and proof.
/// </summary>
public class ConfirmDeliveryViewModel
{
    [Required]
    public string ParcelId { get; set; } = string.Empty;

    public string TrackingId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the name of the person who received the parcel.")]
    [Display(Name = "Received By (Recipient / Representative Name)")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Recipient name must be between 2 and 100 characters.")]
    public string ReceivedBy { get; set; } = string.Empty;

    [Display(Name = "Delivery Proof / Verification Notes")]
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? DeliveryNotes { get; set; }

    [Display(Name = "Signature or OTP Confirmation")]
    public string? SignatureBase64 { get; set; }
}
