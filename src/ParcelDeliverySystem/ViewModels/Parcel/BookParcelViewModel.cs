using System.ComponentModel.DataAnnotations;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Parcels;

/// <summary>
/// Data Transfer & View Model for the complete Parcel Booking workflow.
/// Supports selecting from saved addresses or creating new pickup/delivery locations on the fly.
/// </summary>
public class BookParcelViewModel
{
    // --- Pickup Address Selection / Entry ---
    [Display(Name = "Pickup Source")]
    public string PickupMode { get; set; } = "saved"; // "saved" or "new"

    [Display(Name = "Select Saved Pickup Location")]
    public string? SelectedPickupAddressId { get; set; }

    [Display(Name = "Sender / Contact Name")]
    public string? NewPickupSenderName { get; set; }

    [Display(Name = "Sender Phone Number")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string? NewPickupPhone { get; set; }

    [Display(Name = "Pickup Street Address")]
    public string? NewPickupStreet { get; set; }

    [Display(Name = "City")]
    public string? NewPickupCity { get; set; }

    [Display(Name = "State")]
    public string? NewPickupState { get; set; }

    [Display(Name = "Postal Code")]
    public string? NewPickupPostalCode { get; set; }

    [Display(Name = "Country")]
    public string? NewPickupCountry { get; set; } = "United States";

    public double NewPickupLatitude { get; set; }
    public double NewPickupLongitude { get; set; }
    public bool SaveNewPickupToAddressBook { get; set; } = true;

    // --- Delivery Address Selection / Entry ---
    [Display(Name = "Delivery Destination")]
    public string DeliveryMode { get; set; } = "new"; // "saved" or "new"

    [Display(Name = "Select Saved Destination")]
    public string? SelectedDeliveryAddressId { get; set; }

    [Required(ErrorMessage = "Recipient name is required.")]
    [Display(Name = "Recipient Name")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Recipient phone number is required.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [Display(Name = "Recipient Phone")]
    public string RecipientPhone { get; set; } = string.Empty;

    [Display(Name = "Recipient Email (Optional)")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string? RecipientEmail { get; set; }

    [Display(Name = "Destination Street Address")]
    public string? NewDeliveryStreet { get; set; }

    [Display(Name = "City")]
    public string? NewDeliveryCity { get; set; }

    [Display(Name = "State")]
    public string? NewDeliveryState { get; set; }

    [Display(Name = "Postal Code")]
    public string? NewDeliveryPostalCode { get; set; }

    [Display(Name = "Country")]
    public string? NewDeliveryCountry { get; set; } = "United States";

    public double NewDeliveryLatitude { get; set; }
    public double NewDeliveryLongitude { get; set; }
    public bool SaveNewDeliveryToAddressBook { get; set; } = false;

    // --- Parcel Specifications ---
    [Required(ErrorMessage = "Parcel type is required.")]
    [Display(Name = "Parcel / Package Category")]
    public string ParcelType { get; set; } = "Standard Box";

    [Required(ErrorMessage = "Weight is required.")]
    [Range(0.01, 500.0, ErrorMessage = "Weight must be greater than 0 kg (maximum 500 kg).")]
    [Display(Name = "Scale Weight (kg)")]
    public double Weight { get; set; } = 1.0;

    [Required(ErrorMessage = "Length is required.")]
    [Range(1.0, 300.0, ErrorMessage = "Length must be greater than 0 cm.")]
    [Display(Name = "Length (cm)")]
    public double Length { get; set; } = 15.0;

    [Required(ErrorMessage = "Width is required.")]
    [Range(1.0, 300.0, ErrorMessage = "Width must be greater than 0 cm.")]
    [Display(Name = "Width (cm)")]
    public double Width { get; set; } = 15.0;

    [Required(ErrorMessage = "Height is required.")]
    [Range(1.0, 300.0, ErrorMessage = "Height must be greater than 0 cm.")]
    [Display(Name = "Height (cm)")]
    public double Height { get; set; } = 10.0;

    [Display(Name = "Package Description")]
    public string Description { get; set; } = "Merchandise / Documents";

    [Display(Name = "Special Handling Instructions")]
    public string? SpecialInstructions { get; set; }

    // --- Service Tier Selection ---
    [Required(ErrorMessage = "Delivery type is required.")]
    [Display(Name = "Delivery Speed Tier")]
    public DeliveryType DeliveryType { get; set; } = DeliveryType.Standard;

    // --- Real-time Estimation & Routing Data (Computed by Services) ---
    public double EstimatedDistanceKm { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public decimal EstimatedDeliveryCharge { get; set; }
    public string? FormattedEta { get; set; }
    public string? RouteGeometryJson { get; set; }

    // --- UI Context Dropdowns ---
    public IReadOnlyList<Address> SavedAddresses { get; set; } = new List<Address>();
}
