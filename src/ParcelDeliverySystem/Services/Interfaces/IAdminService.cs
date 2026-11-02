using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.ViewModels.Admin;
using ParcelDeliverySystem.ViewModels.Dashboard;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Service interface governing administrative telemetry, customer management, agent provisioning,
/// multi-criteria parcel search/filtering, and fleet performance reporting.
/// </summary>
public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardMetricsAsync();

    Task<IReadOnlyList<UserListItemViewModel>> GetCustomersAsync(string? searchTerm = null);
    Task<AdminCustomerDetailViewModel?> GetCustomerDetailsAsync(string customerId);

    Task<IReadOnlyList<UserListItemViewModel>> GetAgentsAsync(string? searchTerm = null, AgentAvailabilityStatus? statusFilter = null);
    Task<AdminAgentDetailViewModel?> GetAgentDetailsAsync(string agentId);
    Task<User> CreateAgentAsync(CreateAgentViewModel model);
    Task<bool> UpdateAgentAsync(string agentId, EditAgentViewModel model);
    Task<bool> ToggleUserStatusAsync(string userId);

    Task<ParcelSearchFilterViewModel> SearchParcelsAsync(ParcelSearchFilterViewModel filter);
    Task<AdminReportViewModel> GetAdminReportDataAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
}
