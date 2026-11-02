using Microsoft.Extensions.Logging;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Implements address management with customer authorization checks and auto-geocoding.
/// </summary>
public class AddressService : IAddressService
{
    private readonly IAddressRepository _addressRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<AddressService> _logger;

    public AddressService(
        IAddressRepository addressRepository,
        IGeocodingService geocodingService,
        ILogger<AddressService> logger)
    {
        _addressRepository = addressRepository;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Address>> GetAddressesByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Array.Empty<Address>();
        return await _addressRepository.GetByUserIdAsync(userId);
    }

    public async Task<Address?> GetAddressByIdAsync(string addressId, string userId)
    {
        if (string.IsNullOrWhiteSpace(addressId) || string.IsNullOrWhiteSpace(userId)) return null;

        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId)
        {
            _logger.LogWarning("Unauthorized address access attempt: AddressId {AddressId} by User {UserId}", addressId, userId);
            return null;
        }

        return address;
    }

    public async Task<Address> CreateAddressAsync(Address address, string userId)
    {
        address.UserId = userId;
        address.CreatedAt = DateTime.UtcNow;

        // Auto-geocode if coordinates are not provided
        if (Math.Abs(address.Latitude) < 0.0001 && Math.Abs(address.Longitude) < 0.0001)
        {
            var query = string.IsNullOrWhiteSpace(address.FormattedAddress)
                ? $"{address.StreetAddress}, {address.City}, {address.State} {address.PostalCode}, {address.Country}"
                : address.FormattedAddress;

            var geo = await _geocodingService.GeocodeAddressAsync(query);
            if (geo != null)
            {
                address.Latitude = geo.Latitude;
                address.Longitude = geo.Longitude;
                if (string.IsNullOrWhiteSpace(address.FormattedAddress))
                {
                    address.FormattedAddress = geo.FormattedAddress;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(address.FormattedAddress))
        {
            address.FormattedAddress = $"{address.StreetAddress}, {address.City}, {address.State} {address.PostalCode}, {address.Country}";
        }

        // Check if this is the first address for the user; if so, make it default
        var existing = await _addressRepository.GetByUserIdAsync(userId);
        if (existing.Count == 0 || address.IsDefault)
        {
            await _addressRepository.ClearDefaultAsync(userId);
            address.IsDefault = true;
        }

        return await _addressRepository.CreateAsync(address);
    }

    public async Task<bool> UpdateAddressAsync(Address address, string userId)
    {
        var existing = await _addressRepository.GetByIdAsync(address.Id);
        if (existing == null || existing.UserId != userId)
        {
            _logger.LogWarning("Unauthorized address update: AddressId {AddressId} by User {UserId}", address.Id, userId);
            return false;
        }

        existing.Label = address.Label;
        existing.RecipientName = address.RecipientName;
        existing.PhoneNumber = address.PhoneNumber;
        existing.StreetAddress = address.StreetAddress;
        existing.UnitOrApt = address.UnitOrApt;
        existing.City = address.City;
        existing.State = address.State;
        existing.PostalCode = address.PostalCode;
        existing.Country = address.Country;
        existing.FormattedAddress = $"{address.StreetAddress}, {address.City}, {address.State} {address.PostalCode}, {address.Country}";
        existing.UpdatedAt = DateTime.UtcNow;

        // Re-geocode if requested or updated coordinates
        if (address.Latitude != 0 && address.Longitude != 0)
        {
            existing.Latitude = address.Latitude;
            existing.Longitude = address.Longitude;
        }
        else
        {
            var geo = await _geocodingService.GeocodeAddressAsync(existing.FormattedAddress);
            if (geo != null)
            {
                existing.Latitude = geo.Latitude;
                existing.Longitude = geo.Longitude;
            }
        }

        if (address.IsDefault && !existing.IsDefault)
        {
            await _addressRepository.ClearDefaultAsync(userId);
            existing.IsDefault = true;
        }

        return await _addressRepository.UpdateAsync(existing);
    }

    public async Task<bool> DeleteAddressAsync(string addressId, string userId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId)
        {
            _logger.LogWarning("Unauthorized address deletion: AddressId {AddressId} by User {UserId}", addressId, userId);
            return false;
        }

        return await _addressRepository.DeleteAsync(addressId);
    }

    public async Task<bool> SetDefaultAddressAsync(string addressId, string userId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId);
        if (address == null || address.UserId != userId) return false;

        await _addressRepository.SetAsDefaultAsync(userId, addressId);
        return true;
    }
}
