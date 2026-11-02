using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Repositories.Interfaces;

/// <summary>
/// Address repository interface for managing customer saved delivery locations.
/// </summary>
public interface IAddressRepository : IRepository<Address>
{
    Task<IReadOnlyList<Address>> GetByUserIdAsync(string userId);
    Task<Address?> GetDefaultByUserIdAsync(string userId);
    Task ClearDefaultAsync(string userId);
    Task SetAsDefaultAsync(string userId, string addressId);
}
