using ParcelDeliverySystem.Models.DTOs.Location;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Service contract for address autocomplete and forward geocoding.
/// </summary>
public interface IGeocodingService
{
    Task<IReadOnlyList<AddressSuggestionDto>> AutocompleteAddressAsync(string text);
    Task<AddressSuggestionDto?> GeocodeAddressAsync(string addressText);
    Task<AddressSuggestionDto?> ReverseGeocodeAsync(double latitude, double longitude);
}
