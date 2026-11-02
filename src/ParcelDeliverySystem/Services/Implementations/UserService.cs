using Microsoft.Extensions.Logging;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Dashboard;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Service layer implementation for user profiles, dashboard analytics, and administration.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IParcelRepository _parcelRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IAddressRepository addressRepository,
        IParcelRepository parcelRepository,
        INotificationRepository notificationRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _parcelRepository = parcelRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<UserProfileViewModel?> GetUserProfileAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            AvailabilityStatus = user.AvailabilityStatus,
            VehicleType = user.VehicleType,
            VehiclePlateNumber = user.VehiclePlateNumber,
            DrivingLicenseNumber = user.DrivingLicenseNumber,
            Rating = user.Rating,
            CompletedDeliveriesCount = user.CompletedDeliveriesCount
        };
    }

    public async Task<bool> UpdateProfileAsync(string userId, UserProfileViewModel model)
    {
        return await _userRepository.UpdateProfileAsync(
            userId,
            model.FullName,
            model.PhoneNumber,
            model.VehicleType,
            model.VehiclePlateNumber,
            model.DrivingLicenseNumber
        );
    }

    public async Task<bool> UpdateAgentAvailabilityAsync(string agentId, AgentAvailabilityStatus status)
    {
        _logger.LogInformation("Updating agent {AgentId} availability to {Status}", agentId, status);
        return await _userRepository.UpdateAvailabilityStatusAsync(agentId, status);
    }

    public async Task<bool> UpdateAgentLocationAsync(string agentId, double latitude, double longitude)
    {
        return await _userRepository.UpdateLocationAsync(agentId, latitude, longitude);
    }

    public async Task<IReadOnlyList<UserListItemViewModel>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToListItem).ToList();
    }

    public async Task<IReadOnlyList<UserListItemViewModel>> GetUsersByRoleAsync(UserRole role)
    {
        var users = await _userRepository.GetByRoleAsync(role);
        return users.Select(MapToListItem).ToList();
    }

    public async Task<bool> ToggleUserActiveStatusAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        var newStatus = !user.IsActive;
        return await _userRepository.UpdateActiveStatusAsync(userId, newStatus);
    }

    public async Task<CustomerDashboardViewModel> GetCustomerDashboardDataAsync(string customerId)
    {
        var customer = await _userRepository.GetByIdAsync(customerId) ?? new User();
        var parcels = await _parcelRepository.GetByCustomerIdAsync(customerId);
        var addresses = await _addressRepository.GetByUserIdAsync(customerId);
        var notifications = await _notificationRepository.GetByUserIdAsync(customerId, 5);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(customerId);

        var activeCount = parcels.Count(p => 
            p.Status != ParcelStatus.Delivered && 
            p.Status != ParcelStatus.Cancelled
        );
        var deliveredCount = parcels.Count(p => p.Status == ParcelStatus.Delivered);

        return new CustomerDashboardViewModel
        {
            Customer = customer,
            ActiveBookingsCount = activeCount,
            DeliveredParcelsCount = deliveredCount,
            SavedAddressesCount = addresses.Count,
            UnreadNotificationsCount = (int)unreadCount,
            RecentParcels = parcels.Take(5).ToList(),
            SavedAddresses = addresses,
            RecentNotifications = notifications
        };
    }

    public async Task<AgentDashboardViewModel> GetAgentDashboardDataAsync(string agentId)
    {
        var agent = await _userRepository.GetByIdAsync(agentId) ?? new User();
        var assignedParcels = await _parcelRepository.GetByAgentIdAsync(agentId);
        var availableParcels = await _parcelRepository.GetAvailableParcelsForPickupAsync();
        var notifications = await _notificationRepository.GetByUserIdAsync(agentId, 5);

        var activeDeliveries = assignedParcels.Count(p => 
            p.Status == ParcelStatus.AgentAssigned || 
            p.Status == ParcelStatus.PickedUp || 
            p.Status == ParcelStatus.InTransit || 
            p.Status == ParcelStatus.OutForDelivery
        );

        var today = DateTime.UtcNow.Date;
        var completedToday = assignedParcels.Count(p => 
            p.Status == ParcelStatus.Delivered && 
            p.DeliveredAt.HasValue && 
            p.DeliveredAt.Value.Date == today
        );

        return new AgentDashboardViewModel
        {
            Agent = agent,
            AvailabilityStatus = agent.AvailabilityStatus,
            ActiveDeliveriesCount = activeDeliveries,
            CompletedTodayCount = completedToday,
            AvailablePoolCount = availableParcels.Count,
            Rating = agent.Rating,
            AssignedDeliveries = assignedParcels.Take(5).ToList(),
            AvailableRequests = availableParcels.Take(5).ToList(),
            RecentNotifications = notifications
        };
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardDataAsync()
    {
        var allUsers = await _userRepository.GetAllAsync();
        var recentParcels = await _parcelRepository.GetRecentParcelsAsync(10);

        var totalUsers = allUsers.Count;
        var customersCount = allUsers.Count(u => u.Role == UserRole.Customer);
        var agentsCount = allUsers.Count(u => u.Role == UserRole.DeliveryAgent);
        var availableAgents = allUsers.Where(u => u.Role == UserRole.DeliveryAgent && u.AvailabilityStatus == AgentAvailabilityStatus.Available && u.IsActive).ToList();

        var totalParcels = await _parcelRepository.CountAsync();
        var pendingPickup = await _parcelRepository.CountByStatusAsync(ParcelStatus.PendingPickup);
        var inTransit = await _parcelRepository.CountByStatusAsync(ParcelStatus.InTransit) +
                        await _parcelRepository.CountByStatusAsync(ParcelStatus.PickedUp) +
                        await _parcelRepository.CountByStatusAsync(ParcelStatus.OutForDelivery);
        var delivered = await _parcelRepository.CountByStatusAsync(ParcelStatus.Delivered);

        return new AdminDashboardViewModel
        {
            TotalUsersCount = totalUsers,
            CustomersCount = customersCount,
            AgentsCount = agentsCount,
            AvailableAgentsCount = availableAgents.Count,
            TotalParcelsCount = totalParcels,
            PendingPickupCount = pendingPickup,
            InTransitCount = inTransit,
            DeliveredCount = delivered,
            RecentUsers = allUsers.OrderByDescending(u => u.CreatedAt).Take(8).Select(MapToListItem).ToList(),
            RecentParcels = recentParcels,
            AvailableAgentsList = availableAgents.Select(MapToListItem).ToList()
        };
    }

    private static UserListItemViewModel MapToListItem(User user)
    {
        return new UserListItemViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            AvailabilityStatus = user.Role == UserRole.DeliveryAgent ? user.AvailabilityStatus : null,
            VehicleType = user.VehicleType,
            Rating = user.Rating,
            CompletedDeliveriesCount = user.CompletedDeliveriesCount
        };
    }
}
