using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Business logic contract for managing user address books with ownership validation.
/// </summary>
public interface IAddressService
{
    Task<IReadOnlyList<Address>> GetAddressesByUserIdAsync(string userId);
    Task<Address?> GetAddressByIdAsync(string addressId, string userId);
    Task<Address> CreateAddressAsync(Address address, string userId);
    Task<bool> UpdateAddressAsync(Address address, string userId);
    Task<bool> DeleteAddressAsync(string addressId, string userId);
    Task<bool> SetDefaultAddressAsync(string addressId, string userId);
}
