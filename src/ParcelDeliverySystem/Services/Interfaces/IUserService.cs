using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.ViewModels.Dashboard;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Business service interface for profile updates, role dashboards, and administrative user controls.
/// </summary>
public interface IUserService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<UserProfileViewModel?> GetUserProfileAsync(string userId);
    Task<bool> UpdateProfileAsync(string userId, UserProfileViewModel model);
    Task<bool> UpdateAgentAvailabilityAsync(string agentId, AgentAvailabilityStatus status);
    Task<bool> UpdateAgentLocationAsync(string agentId, double latitude, double longitude);
    Task<IReadOnlyList<UserListItemViewModel>> GetAllUsersAsync();
    Task<IReadOnlyList<UserListItemViewModel>> GetUsersByRoleAsync(UserRole role);
    Task<bool> ToggleUserActiveStatusAsync(string userId);

    Task<CustomerDashboardViewModel> GetCustomerDashboardDataAsync(string customerId);
    Task<AgentDashboardViewModel> GetAgentDashboardDataAsync(string agentId);
    Task<AdminDashboardViewModel> GetAdminDashboardDataAsync();
}
