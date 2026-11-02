using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParcelDeliverySystem.Configurations;
using ParcelDeliverySystem.Models.DTOs.Location;
using ParcelDeliverySystem.Services.Interfaces;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Resilient Location Platform integration supporting Geoapify, OpenStreetMap Nominatim, and OSRM.
/// Works out-of-the-box with free OpenStreetMap data when no Geoapify key is configured.
/// </summary>
public class GeoapifyService : IGeocodingService, IRoutingService
{
    private readonly HttpClient _httpClient;
    private readonly GeoapifySettings _settings;
    private readonly ILogger<GeoapifyService> _logger;

    public GeoapifyService(
        HttpClient httpClient,
        IOptions<GeoapifySettings> settings,
        ILogger<GeoapifyService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ParcelDeliverySystem/1.0 (CustomerDispatch; contact@parceldelivery.local)");
        }
    }

    public async Task<IReadOnlyList<AddressSuggestionDto>> AutocompleteAddressAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2)
        {
            return Array.Empty<AddressSuggestionDto>();
        }

        // 1. Try Geoapify if API key is provided
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey) && _settings.ApiKey != "YOUR_GEOAPIFY_API_KEY")
        {
            try
            {
                var url = $"{_settings.AutocompleteEndpoint}?text={Uri.EscapeDataString(text)}&format=json&apiKey={_settings.ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);

                    var list = new List<AddressSuggestionDto>();
                    if (doc.RootElement.TryGetProperty("results", out var results))
                    {
                        foreach (var item in results.EnumerateArray())
                        {
                            var suggestion = new AddressSuggestionDto
                            {
                                FormattedAddress = item.TryGetProperty("formatted", out var f) ? f.GetString() ?? string.Empty : string.Empty,
                                Street = item.TryGetProperty("street", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                                City = item.TryGetProperty("city", out var c) ? c.GetString() ?? string.Empty : string.Empty,
                                State = item.TryGetProperty("state", out var st) ? st.GetString() ?? string.Empty : string.Empty,
                                PostalCode = item.TryGetProperty("postcode", out var p) ? p.GetString() ?? string.Empty : string.Empty,
                                Country = item.TryGetProperty("country", out var co) ? co.GetString() ?? string.Empty : string.Empty,
                                Latitude = item.TryGetProperty("lat", out var lat) ? lat.GetDouble() : 0,
                                Longitude = item.TryGetProperty("lon", out var lon) ? lon.GetDouble() : 0
                            };

                            if (!string.IsNullOrEmpty(suggestion.FormattedAddress))
                            {
                                list.Add(suggestion);
                            }
                        }
                    }

                    if (list.Count > 0) return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geoapify autocomplete failed. Falling back to OpenStreetMap Nominatim.");
            }
        }

        // 2. OpenStreetMap Nominatim Free Geocoder
        try
        {
            var osmUrl = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(text)}&format=json&addressdetails=1&limit=5";
            using var request = new HttpRequestMessage(HttpMethod.Get, osmUrl);
            var osmResponse = await _httpClient.SendAsync(request);

            if (osmResponse.IsSuccessStatusCode)
            {
                var content = await osmResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var list = new List<AddressSuggestionDto>();
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        var displayName = item.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;
                        var latStr = item.TryGetProperty("lat", out var lt) ? lt.GetString() : "0";
                        var lonStr = item.TryGetProperty("lon", out var ln) ? ln.GetString() : "0";

                        double.TryParse(latStr, CultureInfo.InvariantCulture, out var lat);
                        double.TryParse(lonStr, CultureInfo.InvariantCulture, out var lon);

                        string street = string.Empty;
                        string city = string.Empty;
                        string state = string.Empty;
                        string postcode = string.Empty;
                        string country = string.Empty;

                        if (item.TryGetProperty("address", out var addr))
                        {
                            street = GetFirstNonEmptyProperty(addr, "road", "pedestrian", "suburb", "neighbourhood", "house_number");
                            city = GetFirstNonEmptyProperty(addr, "city", "town", "village", "municipality", "county");
                            state = GetFirstNonEmptyProperty(addr, "state", "region", "province");
                            postcode = GetFirstNonEmptyProperty(addr, "postcode");
                            country = GetFirstNonEmptyProperty(addr, "country");
                        }

                        if (!string.IsNullOrEmpty(displayName))
                        {
                            list.Add(new AddressSuggestionDto
                            {
                                FormattedAddress = displayName,
                                Street = string.IsNullOrEmpty(street) ? displayName.Split(',').FirstOrDefault()?.Trim() ?? string.Empty : street,
                                City = city,
                                State = state,
                                PostalCode = postcode,
                                Country = country,
                                Latitude = lat,
                                Longitude = lon
                            });
                        }
                    }
                }

                if (list.Count > 0) return list;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenStreetMap Nominatim search failed. Using local fallback.");
        }

        // 3. Resilient Local Sample Fallback
        return GetLocalAddressSuggestions(text);
    }

    public async Task<AddressSuggestionDto?> GeocodeAddressAsync(string addressText)
    {
        if (string.IsNullOrWhiteSpace(addressText)) return null;

        var suggestions = await AutocompleteAddressAsync(addressText);
        return suggestions.FirstOrDefault();
    }

    public async Task<AddressSuggestionDto?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        if (Math.Abs(latitude) < 0.0001 && Math.Abs(longitude) < 0.0001)
        {
            return null;
        }

        // 1. Try Geoapify Reverse Geocoding if Key is configured
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey) && _settings.ApiKey != "YOUR_GEOAPIFY_API_KEY")
        {
            try
            {
                var latStr = latitude.ToString(CultureInfo.InvariantCulture);
                var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
                var url = $"https://api.geoapify.com/v1/geocode/reverse?lat={latStr}&lon={lonStr}&format=json&apiKey={_settings.ApiKey}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);

                    if (doc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                    {
                        var item = results[0];
                        return new AddressSuggestionDto
                        {
                            FormattedAddress = item.TryGetProperty("formatted", out var f) ? f.GetString() ?? string.Empty : string.Empty,
                            Street = item.TryGetProperty("street", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                            City = item.TryGetProperty("city", out var c) ? c.GetString() ?? string.Empty : string.Empty,
                            State = item.TryGetProperty("state", out var st) ? st.GetString() ?? string.Empty : string.Empty,
                            PostalCode = item.TryGetProperty("postcode", out var p) ? p.GetString() ?? string.Empty : string.Empty,
                            Country = item.TryGetProperty("country", out var co) ? co.GetString() ?? string.Empty : string.Empty,
                            Latitude = latitude,
                            Longitude = longitude
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geoapify reverse geocoding failed. Falling back to OpenStreetMap.");
            }
        }

        // 2. OpenStreetMap Nominatim Free Reverse Geocoder
        try
        {
            var latStr = latitude.ToString(CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
            var osmUrl = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=json&addressdetails=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, osmUrl);
            var osmResponse = await _httpClient.SendAsync(request);

            if (osmResponse.IsSuccessStatusCode)
            {
                var content = await osmResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var displayName = doc.RootElement.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;
                string street = string.Empty;
                string city = string.Empty;
                string state = string.Empty;
                string postcode = string.Empty;
                string country = string.Empty;

                if (doc.RootElement.TryGetProperty("address", out var addr))
                {
                    street = GetFirstNonEmptyProperty(addr, "road", "pedestrian", "suburb", "neighbourhood", "house_number");
                    city = GetFirstNonEmptyProperty(addr, "city", "town", "village", "municipality", "county");
                    state = GetFirstNonEmptyProperty(addr, "state", "region", "province");
                    postcode = GetFirstNonEmptyProperty(addr, "postcode");
                    country = GetFirstNonEmptyProperty(addr, "country");
                }

                if (!string.IsNullOrEmpty(displayName))
                {
                    return new AddressSuggestionDto
                    {
                        FormattedAddress = displayName,
                        Street = string.IsNullOrEmpty(street) ? displayName.Split(',').FirstOrDefault()?.Trim() ?? string.Empty : street,
                        City = city,
                        State = state,
                        PostalCode = postcode,
                        Country = country,
                        Latitude = latitude,
                        Longitude = longitude
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenStreetMap Nominatim reverse geocode failed.");
        }

        // 3. Fallback Coordinate descriptor
        return new AddressSuggestionDto
        {
            FormattedAddress = $"Location ({latitude:F4}, {longitude:F4})",
            Street = $"Point ({latitude:F4}, {longitude:F4})",
            City = "Local Area",
            State = "Region",
            PostalCode = "00000",
            Country = "Default",
            Latitude = latitude,
            Longitude = longitude
        };
    }

    public async Task<RouteCalculationResultDto> CalculateRouteAsync(double fromLat, double fromLon, double toLat, double toLon)
    {
        // Check for identical coordinates
        if (Math.Abs(fromLat - toLat) < 0.0001 && Math.Abs(fromLon - toLon) < 0.0001)
        {
            return new RouteCalculationResultDto
            {
                Success = true,
                DistanceKm = 0.5,
                DurationMinutes = 5,
                FormattedDuration = "5 mins",
                GeometryGeoJson = CreateGeoJsonLine(fromLat, fromLon, toLat, toLon)
            };
        }

        // 1. Try Geoapify Routing if API key is provided
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey) && _settings.ApiKey != "YOUR_GEOAPIFY_API_KEY")
        {
            try
            {
                var waypoints = $"{fromLat.ToString(CultureInfo.InvariantCulture)},{fromLon.ToString(CultureInfo.InvariantCulture)}|{toLat.ToString(CultureInfo.InvariantCulture)},{toLon.ToString(CultureInfo.InvariantCulture)}";
                var url = $"{_settings.RoutingEndpoint}?waypoints={waypoints}&mode=drive&apiKey={_settings.ApiKey}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);

                    if (doc.RootElement.TryGetProperty("features", out var features) && features.GetArrayLength() > 0)
                    {
                        var feature = features[0];
                        var properties = feature.GetProperty("properties");

                        var distanceMeters = properties.GetProperty("distance").GetDouble();
                        var timeSeconds = properties.GetProperty("time").GetDouble();

                        var geometry = feature.GetProperty("geometry");
                        var geometryJson = geometry.GetRawText();

                        var distanceKm = Math.Round(distanceMeters / 1000.0, 2);
                        var durationMinutes = (int)Math.Max(5, Math.Ceiling(timeSeconds / 60.0));

                        return new RouteCalculationResultDto
                        {
                            Success = true,
                            DistanceKm = distanceKm,
                            DurationMinutes = durationMinutes,
                            FormattedDuration = FormatDuration(durationMinutes),
                            GeometryGeoJson = geometryJson,
                            IsFallback = false
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geoapify routing failed. Trying OSRM public router.");
            }
        }

        // 2. Open Source Routing Machine (OSRM) Free Road Navigation
        try
        {
            var p1 = $"{fromLon.ToString(CultureInfo.InvariantCulture)},{fromLat.ToString(CultureInfo.InvariantCulture)}";
            var p2 = $"{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";
            var osrmUrl = $"https://router.project-osrm.org/route/v1/driving/{p1};{p2}?overview=full&geometries=geojson";

            using var request = new HttpRequestMessage(HttpMethod.Get, osrmUrl);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                {
                    var route = routes[0];
                    var distanceMeters = route.GetProperty("distance").GetDouble();
                    var durationSeconds = route.GetProperty("duration").GetDouble();
                    var geometryJson = route.GetProperty("geometry").GetRawText();

                    var distanceKm = Math.Round(distanceMeters / 1000.0, 2);
                    var durationMinutes = (int)Math.Max(5, Math.Ceiling(durationSeconds / 60.0));

                    return new RouteCalculationResultDto
                    {
                        Success = true,
                        DistanceKm = distanceKm,
                        DurationMinutes = durationMinutes,
                        FormattedDuration = FormatDuration(durationMinutes),
                        GeometryGeoJson = geometryJson,
                        IsFallback = false
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM routing failed. Using Haversine fallback.");
        }

        // 3. Mathematical Haversine Fallback
        return CalculateHaversineFallback(fromLat, fromLon, toLat, toLon);
    }

    private static string GetFirstNonEmptyProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            if (element.TryGetProperty(prop, out var val))
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return str.Trim();
                }
            }
        }
        return string.Empty;
    }

    private static RouteCalculationResultDto CalculateHaversineFallback(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var straightLineKm = EarthRadiusKm * c;

        // Multiply by 1.25 road winding factor
        var roadDistanceKm = Math.Max(1.0, Math.Round(straightLineKm * 1.25, 2));

        // Average 35 km/h urban speed
        var durationMinutes = (int)Math.Max(10, Math.Ceiling((roadDistanceKm / 35.0) * 60));

        return new RouteCalculationResultDto
        {
            Success = true,
            DistanceKm = roadDistanceKm,
            DurationMinutes = durationMinutes,
            FormattedDuration = FormatDuration(durationMinutes),
            GeometryGeoJson = CreateGeoJsonLine(lat1, lon1, lat2, lon2),
            IsFallback = true
        };
    }

    private static string CreateGeoJsonLine(double lat1, double lon1, double lat2, double lon2)
    {
        return JsonSerializer.Serialize(new
        {
            type = "LineString",
            coordinates = new[]
            {
                new[] { lon1, lat1 },
                new[] { lon2, lat2 }
            }
        });
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60) return $"{minutes} mins";
        var hours = minutes / 60;
        var rem = minutes % 60;
        return rem > 0 ? $"{hours} hr {rem} mins" : $"{hours} hr";
    }

    private static IReadOnlyList<AddressSuggestionDto> GetLocalAddressSuggestions(string query)
    {
        var samplePlaces = new List<AddressSuggestionDto>
        {
            new() { FormattedAddress = "742 Evergreen Terrace, Springfield, OR 97477", Street = "742 Evergreen Terrace", City = "Springfield", State = "OR", PostalCode = "97477", Country = "United States", Latitude = 44.0462, Longitude = -123.0220 },
            new() { FormattedAddress = "350 5th Avenue (Empire State Building), New York, NY 10118", Street = "350 5th Avenue", City = "New York", State = "NY", PostalCode = "10118", Country = "United States", Latitude = 40.7484, Longitude = -73.9857 },
            new() { FormattedAddress = "1060 W Addison St (Wrigley Field), Chicago, IL 60613", Street = "1060 W Addison St", City = "Chicago", State = "IL", PostalCode = "60613", Country = "United States", Latitude = 41.9484, Longitude = -87.6553 },
            new() { FormattedAddress = "1 Apple Park Way, Cupertino, CA 95014", Street = "1 Apple Park Way", City = "Cupertino", State = "CA", PostalCode = "95014", Country = "United States", Latitude = 37.3346, Longitude = -122.0090 },
            new() { FormattedAddress = "221B Baker Street, Marylebone, London NW1 6XE", Street = "221B Baker Street", City = "London", State = "London", PostalCode = "NW1 6XE", Country = "United Kingdom", Latitude = 51.5237, Longitude = -0.1585 }
        };

        var lower = query.ToLowerInvariant();
        var matches = samplePlaces.Where(p => p.FormattedAddress.ToLowerInvariant().Contains(lower)).ToList();

        if (matches.Count == 0)
        {
            matches.Add(new AddressSuggestionDto
            {
                FormattedAddress = $"{query}, Springfield, OR 97477",
                Street = query,
                City = "Springfield",
                State = "OR",
                PostalCode = "97477",
                Country = "United States",
                Latitude = 44.0462 + (query.Length % 10) * 0.01,
                Longitude = -123.0220 + (query.Length % 10) * 0.01
            });
        }

        return matches;
    }
}
