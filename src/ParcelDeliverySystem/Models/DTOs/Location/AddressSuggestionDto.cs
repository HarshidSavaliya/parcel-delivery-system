namespace ParcelDeliverySystem.Models.DTOs.Location;

/// <summary>
/// Address autocomplete suggestion item returned by Geoapify or internal geocoder.
/// </summary>
public class AddressSuggestionDto
{
    public string FormattedAddress { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
