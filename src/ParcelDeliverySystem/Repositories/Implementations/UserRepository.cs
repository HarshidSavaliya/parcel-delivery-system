using MongoDB.Driver;
using ParcelDeliverySystem.Data;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Repositories.Implementations;

/// <summary>
/// MongoDB implementation of IUserRepository.
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IMongoDbContext context) : base(context.Users)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _collection.Find(u => u.Email.ToLower() == normalizedEmail).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role)
    {
        return await _collection.Find(u => u.Role == role).ToListAsync();
    }

    public async Task<IReadOnlyList<User>> GetAvailableAgentsAsync()
    {
        return await _collection.Find(u => 
            u.Role == UserRole.DeliveryAgent && 
            u.IsActive && 
            u.AvailabilityStatus == AgentAvailabilityStatus.Available
        ).ToListAsync();
    }

    public async Task<bool> IsEmailTakenAsync(string email, string? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(excludeUserId))
        {
            return await _collection.Find(u => u.Email.ToLower() == normalizedEmail).AnyAsync();
        }

        return await _collection.Find(u => u.Email.ToLower() == normalizedEmail && u.Id != excludeUserId).AnyAsync();
    }

    public async Task<bool> UpdateAvailabilityStatusAsync(string agentId, AgentAvailabilityStatus status)
    {
        var update = Builders<User>.Update
            .Set(u => u.AvailabilityStatus, status)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(u => u.Id == agentId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateLocationAsync(string agentId, double latitude, double longitude)
    {
        var update = Builders<User>.Update
            .Set(u => u.CurrentLatitude, latitude)
            .Set(u => u.CurrentLongitude, longitude)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(u => u.Id == agentId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateLastLoginAsync(string userId)
    {
        var update = Builders<User>.Update
            .Set(u => u.LastLoginAt, DateTime.UtcNow)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(u => u.Id == userId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdatePasswordHashAsync(string userId, string newPasswordHash)
    {
        var update = Builders<User>.Update
            .Set(u => u.PasswordHash, newPasswordHash)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(u => u.Id == userId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateProfileAsync(string userId, string fullName, string phoneNumber, string? vehicleType = null, string? vehiclePlateNumber = null, string? drivingLicenseNumber = null)
    {
        var update = Builders<User>.Update
            .Set(u => u.FullName, fullName.Trim())
            .Set(u => u.PhoneNumber, phoneNumber.Trim())
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        if (vehicleType != null)
            update = update.Set(u => u.VehicleType, vehicleType.Trim());

        if (vehiclePlateNumber != null)
            update = update.Set(u => u.VehiclePlateNumber, vehiclePlateNumber.Trim());

        if (drivingLicenseNumber != null)
            update = update.Set(u => u.DrivingLicenseNumber, drivingLicenseNumber.Trim());

        var result = await _collection.UpdateOneAsync(u => u.Id == userId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateAgentProfileAsync(string agentId, string fullName, string phoneNumber, string? address, double? latitude, double? longitude, string? vehicleType = null, string? vehiclePlateNumber = null, string? drivingLicenseNumber = null)
    {
        var update = Builders<User>.Update
            .Set(u => u.FullName, fullName.Trim())
            .Set(u => u.PhoneNumber, phoneNumber.Trim())
            .Set(u => u.Address, address?.Trim())
            .Set(u => u.CurrentLatitude, latitude)
            .Set(u => u.CurrentLongitude, longitude)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        if (vehicleType != null)
            update = update.Set(u => u.VehicleType, vehicleType.Trim());

        if (vehiclePlateNumber != null)
            update = update.Set(u => u.VehiclePlateNumber, vehiclePlateNumber.Trim());

        if (drivingLicenseNumber != null)
            update = update.Set(u => u.DrivingLicenseNumber, drivingLicenseNumber.Trim());

        var result = await _collection.UpdateOneAsync(u => u.Id == agentId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> IncrementCompletedDeliveriesAsync(string agentId)
    {
        var update = Builders<User>.Update
            .Inc(u => u.CompletedDeliveriesCount, 1)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(u => u.Id == agentId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateActiveStatusAsync(string userId, bool isActive)
    {
        var update = Builders<User>.Update
            .Set(u => u.IsActive, isActive)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(u => u.Id == userId, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }
}
