using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Repositories.Interfaces;

/// <summary>
/// User repository interface extending generic repository with domain-specific user queries.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role);
    Task<IReadOnlyList<User>> GetAvailableAgentsAsync();
    Task<bool> IsEmailTakenAsync(string email, string? excludeUserId = null);
    Task<bool> UpdateAvailabilityStatusAsync(string agentId, AgentAvailabilityStatus status);
    Task<bool> UpdateLocationAsync(string agentId, double latitude, double longitude);
    Task<bool> UpdateLastLoginAsync(string userId);
    Task<bool> UpdatePasswordHashAsync(string userId, string newPasswordHash);
    Task<bool> UpdateProfileAsync(string userId, string fullName, string phoneNumber, string? vehicleType = null, string? vehiclePlateNumber = null, string? drivingLicenseNumber = null);
    Task<bool> UpdateAgentProfileAsync(string agentId, string fullName, string phoneNumber, string? address, double? latitude, double? longitude, string? vehicleType = null, string? vehiclePlateNumber = null, string? drivingLicenseNumber = null);
    Task<bool> IncrementCompletedDeliveriesAsync(string agentId);
    Task<bool> UpdateActiveStatusAsync(string userId, bool isActive);
}
