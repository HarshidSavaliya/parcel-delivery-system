namespace ParcelDeliverySystem.Configurations;

/// <summary>
/// Strongly-typed settings for Geoapify Location Platform integration.
/// Binds directly to the "Geoapify" section of appsettings.json.
/// </summary>
public class GeoapifySettings
{
    public const string SectionName = "Geoapify";

    public string ApiKey { get; set; } = string.Empty;
    public string GeocodeEndpoint { get; set; } = "https://api.geoapify.com/v1/geocode/search";
    public string AutocompleteEndpoint { get; set; } = "https://api.geoapify.com/v1/geocode/autocomplete";
    public string RoutingEndpoint { get; set; } = "https://api.geoapify.com/v1/routing";
}
