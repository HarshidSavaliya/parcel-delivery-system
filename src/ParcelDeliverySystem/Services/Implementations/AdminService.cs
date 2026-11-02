using Microsoft.Extensions.Logging;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Admin;
using ParcelDeliverySystem.ViewModels.Dashboard;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Service implementation managing central admin telemetry, user/agent provisioning,
/// multi-parameter parcel searches, and fleet performance reporting.
/// </summary>
public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IParcelRepository _parcelRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IAgentAssignmentRepository _assignmentRepository;
    private readonly IDeliveryRecordRepository _deliveryRecordRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUserRepository userRepository,
        IParcelRepository parcelRepository,
        IAddressRepository addressRepository,
        IAgentAssignmentRepository assignmentRepository,
        IDeliveryRecordRepository deliveryRecordRepository,
        IPasswordHasherService passwordHasher,
        ILogger<AdminService> logger)
    {
        _userRepository = userRepository;
        _parcelRepository = parcelRepository;
        _addressRepository = addressRepository;
        _assignmentRepository = assignmentRepository;
        _deliveryRecordRepository = deliveryRecordRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AdminDashboardViewModel> GetDashboardMetricsAsync()
    {
        var allUsers = await _userRepository.GetAllAsync();
        var allParcels = await _parcelRepository.GetAllAsync();

        var customers = allUsers.Where(u => u.Role == UserRole.Customer).ToList();
        var agents = allUsers.Where(u => u.Role == UserRole.DeliveryAgent).ToList();
        var availableAgents = agents.Where(a => a.AvailabilityStatus == AgentAvailabilityStatus.Available && a.IsActive).ToList();

        var deliveredCount = allParcels.Count(p => p.Status == ParcelStatus.Delivered);
        var pendingCount = allParcels.Count(p => 
            p.Status == ParcelStatus.Booked || 
            p.Status == ParcelStatus.PendingPickup || 
            p.Status == ParcelStatus.AgentAssigned);
        var inTransitCount = allParcels.Count(p => 
            p.Status == ParcelStatus.PickedUp || 
            p.Status == ParcelStatus.InTransit || 
            p.Status == ParcelStatus.OutForDelivery);
        var cancelledCount = allParcels.Count(p => p.Status == ParcelStatus.Cancelled);
        var failedCount = allParcels.Count(p => 
            p.Status == ParcelStatus.DeliveryFailed || 
            p.Status == ParcelStatus.FailedAttempt);

        var recentUsers = allUsers
            .OrderByDescending(u => u.CreatedAt)
            .Take(8)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                IsActive = u.IsActive,
                AvailabilityStatus = u.Role == UserRole.DeliveryAgent ? u.AvailabilityStatus : null,
                CreatedAt = u.CreatedAt
            }).ToList();

        var recentParcels = allParcels
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToList();

        var availableAgentsList = availableAgents
            .Select(a => new UserListItemViewModel
            {
                Id = a.Id,
                FullName = a.FullName,
                Email = a.Email,
                PhoneNumber = a.PhoneNumber,
                Role = a.Role,
                IsActive = a.IsActive,
                AvailabilityStatus = a.AvailabilityStatus,
                CreatedAt = a.CreatedAt
            }).ToList();

        return new AdminDashboardViewModel
        {
            TotalUsersCount = allUsers.Count,
            CustomersCount = customers.Count,
            AgentsCount = agents.Count,
            AvailableAgentsCount = availableAgents.Count,
            TotalParcelsCount = allParcels.Count,
            PendingPickupCount = pendingCount,
            InTransitCount = inTransitCount,
            DeliveredCount = deliveredCount,
            CancelledCount = cancelledCount,
            FailedDeliveriesCount = failedCount,
            RecentUsers = recentUsers,
            RecentParcels = recentParcels,
            AvailableAgentsList = availableAgentsList
        };
    }

    public async Task<IReadOnlyList<UserListItemViewModel>> GetCustomersAsync(string? searchTerm = null)
    {
        var allUsers = await _userRepository.GetByRoleAsync(UserRole.Customer);
        var query = allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(u => 
                u.FullName.ToLowerInvariant().Contains(term) ||
                u.Email.ToLowerInvariant().Contains(term) ||
                u.PhoneNumber.Contains(term));
        }

        return query.OrderByDescending(u => u.CreatedAt).Select(u => new UserListItemViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    public async Task<AdminCustomerDetailViewModel?> GetCustomerDetailsAsync(string customerId)
    {
        var user = await _userRepository.GetByIdAsync(customerId);
        if (user == null || user.Role != UserRole.Customer)
        {
            return null;
        }

        var addresses = await _addressRepository.GetByUserIdAsync(customerId);
        var parcels = await _parcelRepository.GetByCustomerIdAsync(customerId);

        var totalSpent = parcels.Where(p => p.Status != ParcelStatus.Cancelled).Sum(p => p.DeliveryCharge);
        var completedCount = parcels.Count(p => p.Status == ParcelStatus.Delivered);

        return new AdminCustomerDetailViewModel
        {
            Customer = user,
            SavedAddresses = addresses,
            BookingHistory = parcels,
            TotalSpent = totalSpent,
            CompletedBookingsCount = completedCount
        };
    }

    public async Task<IReadOnlyList<UserListItemViewModel>> GetAgentsAsync(string? searchTerm = null, AgentAvailabilityStatus? statusFilter = null)
    {
        var allAgents = await _userRepository.GetByRoleAsync(UserRole.DeliveryAgent);
        var query = allAgents.AsEnumerable();

        if (statusFilter.HasValue)
        {
            query = query.Where(a => a.AvailabilityStatus == statusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(a => 
                a.FullName.ToLowerInvariant().Contains(term) ||
                a.Email.ToLowerInvariant().Contains(term) ||
                a.PhoneNumber.Contains(term) ||
                (a.VehiclePlateNumber != null && a.VehiclePlateNumber.ToLowerInvariant().Contains(term)) ||
                (a.VehicleType != null && a.VehicleType.ToLowerInvariant().Contains(term)));
        }

        return query.OrderByDescending(a => a.CreatedAt).Select(a => new UserListItemViewModel
        {
            Id = a.Id,
            FullName = a.FullName,
            Email = a.Email,
            PhoneNumber = a.PhoneNumber,
            Role = a.Role,
            IsActive = a.IsActive,
            AvailabilityStatus = a.AvailabilityStatus,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    public async Task<AdminAgentDetailViewModel?> GetAgentDetailsAsync(string agentId)
    {
        var agent = await _userRepository.GetByIdAsync(agentId);
        if (agent == null || agent.Role != UserRole.DeliveryAgent)
        {
            return null;
        }

        var parcels = await _parcelRepository.GetByAgentIdAsync(agentId);
        var assignments = await _assignmentRepository.GetByAgentIdAsync(agentId);

        var activeDeliveries = parcels.Where(p => 
            p.Status == ParcelStatus.AgentAssigned || 
            p.Status == ParcelStatus.PickedUp || 
            p.Status == ParcelStatus.InTransit || 
            p.Status == ParcelStatus.OutForDelivery).ToList();

        var completedDeliveries = parcels.Where(p => 
            p.Status == ParcelStatus.Delivered || 
            p.Status == ParcelStatus.DeliveryFailed || 
            p.Status == ParcelStatus.Cancelled).ToList();

        var totalRuns = parcels.Count;
        var deliveredRuns = parcels.Count(p => p.Status == ParcelStatus.Delivered);
        var successRate = totalRuns > 0 ? Math.Round(((double)deliveredRuns / totalRuns) * 100.0, 1) : 100.0;

        return new AdminAgentDetailViewModel
        {
            Agent = agent,
            ActiveDeliveries = activeDeliveries,
            CompletedDeliveries = completedDeliveries,
            RecentAssignments = assignments,
            SuccessRate = successRate
        };
    }

    public async Task<User> CreateAgentAsync(CreateAgentViewModel model)
    {
        var emailTaken = await _userRepository.IsEmailTakenAsync(model.Email);
        if (emailTaken)
        {
            throw new InvalidOperationException($"The email address '{model.Email}' is already registered in the system.");
        }

        var agent = new User
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            PhoneNumber = model.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.HashPassword(model.Password),
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            Address = model.Address?.Trim(),
            VehicleType = model.VehicleType?.Trim(),
            VehiclePlateNumber = model.VehiclePlateNumber?.Trim(),
            DrivingLicenseNumber = model.DrivingLicenseNumber?.Trim(),
            AvailabilityStatus = model.AvailabilityStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _userRepository.CreateAsync(agent);
        _logger.LogInformation("Admin created new delivery agent account: {Email}", created.Email);
        return created;
    }

    public async Task<bool> UpdateAgentAsync(string agentId, EditAgentViewModel model)
    {
        var agent = await _userRepository.GetByIdAsync(agentId);
        if (agent == null || agent.Role != UserRole.DeliveryAgent)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var newHash = _passwordHasher.HashPassword(model.NewPassword);
            await _userRepository.UpdatePasswordHashAsync(agentId, newHash);
        }

        await _userRepository.UpdateActiveStatusAsync(agentId, model.IsActive);
        await _userRepository.UpdateAvailabilityStatusAsync(agentId, model.AvailabilityStatus);

        return await _userRepository.UpdateAgentProfileAsync(
            agentId,
            model.FullName,
            model.PhoneNumber,
            model.Address,
            agent.CurrentLatitude,
            agent.CurrentLongitude,
            model.VehicleType,
            model.VehiclePlateNumber,
            model.DrivingLicenseNumber
        );
    }

    public async Task<bool> ToggleUserStatusAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        var newStatus = !user.IsActive;
        return await _userRepository.UpdateActiveStatusAsync(userId, newStatus);
    }

    public async Task<ParcelSearchFilterViewModel> SearchParcelsAsync(ParcelSearchFilterViewModel filter)
    {
        var allParcels = await _parcelRepository.GetAllAsync();
        var allAgents = await _userRepository.GetByRoleAsync(UserRole.DeliveryAgent);

        var query = allParcels.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(p => 
                p.TrackingId.ToLowerInvariant().Contains(term) ||
                (p.DeliveryAddress?.RecipientName != null && p.DeliveryAddress.RecipientName.ToLowerInvariant().Contains(term)) ||
                (p.DeliveryAddress?.PhoneNumber != null && p.DeliveryAddress.PhoneNumber.Contains(term)) ||
                (p.PickupAddress?.City != null && p.PickupAddress.City.ToLowerInvariant().Contains(term)) ||
                (p.DeliveryAddress?.City != null && p.DeliveryAddress.City.ToLowerInvariant().Contains(term)));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(p => p.Status == filter.Status.Value);
        }

        if (filter.DeliveryType.HasValue)
        {
            query = query.Where(p => p.DeliveryType == filter.DeliveryType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AgentId))
        {
            query = query.Where(p => p.AssignedAgentId == filter.AgentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.CustomerId))
        {
            query = query.Where(p => p.CustomerId == filter.CustomerId);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(p => p.BookingDate >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            var endOfDay = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(p => p.BookingDate <= endOfDay);
        }

        filter.Parcels = query.OrderByDescending(p => p.BookingDate).ToList();
        filter.AgentsList = allAgents;
        return filter;
    }

    public async Task<AdminReportViewModel> GetAdminReportDataAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var allParcels = await _parcelRepository.GetAllAsync();
        var allAgents = await _userRepository.GetByRoleAsync(UserRole.DeliveryAgent);

        var parcelsQuery = allParcels.AsEnumerable();

        if (dateFrom.HasValue)
        {
            parcelsQuery = parcelsQuery.Where(p => p.BookingDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            var endOfDay = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            parcelsQuery = parcelsQuery.Where(p => p.BookingDate <= endOfDay);
        }

        var parcelsList = parcelsQuery.ToList();

        var totalCount = parcelsList.Count;
        var deliveredCount = parcelsList.Count(p => p.Status == ParcelStatus.Delivered);
        var pendingCount = parcelsList.Count(p => 
            p.Status == ParcelStatus.Booked || 
            p.Status == ParcelStatus.PendingPickup || 
            p.Status == ParcelStatus.AgentAssigned);
        var inTransitCount = parcelsList.Count(p => 
            p.Status == ParcelStatus.PickedUp || 
            p.Status == ParcelStatus.InTransit || 
            p.Status == ParcelStatus.OutForDelivery);
        var cancelledCount = parcelsList.Count(p => p.Status == ParcelStatus.Cancelled);
        var failedCount = parcelsList.Count(p => 
            p.Status == ParcelStatus.DeliveryFailed || 
            p.Status == ParcelStatus.FailedAttempt);

        var revenue = parcelsList.Where(p => p.Status != ParcelStatus.Cancelled).Sum(p => p.DeliveryCharge);

        // Date breakdown (daily volume)
        var dailyBreakdown = parcelsList
            .GroupBy(p => p.BookingDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DateVolumeReportItem
            {
                Date = g.Key,
                BookedCount = g.Count(),
                DeliveredCount = g.Count(p => p.Status == ParcelStatus.Delivered),
                CancelledCount = g.Count(p => p.Status == ParcelStatus.Cancelled),
                Revenue = g.Where(p => p.Status != ParcelStatus.Cancelled).Sum(p => p.DeliveryCharge)
            }).ToList();

        // Agent performance metrics
        var agentPerformance = new List<AgentPerformanceReportItemViewModel>();
        foreach (var agent in allAgents)
        {
            var agentParcels = allParcels.Where(p => p.AssignedAgentId == agent.Id).ToList();
            var assignedCount = agentParcels.Count;
            var agentDelivered = agentParcels.Count(p => p.Status == ParcelStatus.Delivered);
            var agentFailed = agentParcels.Count(p => p.Status == ParcelStatus.DeliveryFailed || p.Status == ParcelStatus.FailedAttempt);
            var agentActive = agentParcels.Count(p => 
                p.Status == ParcelStatus.AgentAssigned || 
                p.Status == ParcelStatus.PickedUp || 
                p.Status == ParcelStatus.InTransit || 
                p.Status == ParcelStatus.OutForDelivery);

            agentPerformance.Add(new AgentPerformanceReportItemViewModel
            {
                AgentId = agent.Id,
                AgentName = agent.FullName,
                Email = agent.Email,
                PhoneNumber = agent.PhoneNumber,
                VehicleType = agent.VehicleType ?? "Standard",
                AvailabilityStatus = agent.AvailabilityStatus,
                IsActive = agent.IsActive,
                AssignedDeliveriesCount = assignedCount,
                CompletedDeliveriesCount = agentDelivered,
                FailedDeliveriesCount = agentFailed,
                ActiveDeliveriesCount = agentActive,
                Rating = agent.Rating
            });
        }

        return new AdminReportViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalParcelsCount = totalCount,
            DeliveredParcelsCount = deliveredCount,
            PendingParcelsCount = pendingCount,
            InTransitParcelsCount = inTransitCount,
            CancelledParcelsCount = cancelledCount,
            FailedDeliveriesCount = failedCount,
            TotalRevenueGenerated = revenue,
            DailyVolumeBreakdown = dailyBreakdown,
            AgentPerformanceList = agentPerformance.OrderByDescending(a => a.CompletedDeliveriesCount).ToList()
        };
    }
}
