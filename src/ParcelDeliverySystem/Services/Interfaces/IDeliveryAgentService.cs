using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.ViewModels.Agent;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Service interface governing delivery agent operations, proximity request dispatching, 
/// state lifecycle transitions, and availability state management.
/// </summary>
public interface IDeliveryAgentService
{
    Task<AgentProfileViewModel?> GetAgentProfileAsync(string agentId);
    Task<bool> UpdateAgentProfileAsync(string agentId, AgentProfileViewModel model);
    Task<bool> UpdateAvailabilityStatusAsync(string agentId, AgentAvailabilityStatus status);
    Task<bool> UpdateLocationAsync(string agentId, double latitude, double longitude);

    Task<IReadOnlyList<AvailableRequestViewModel>> GetAvailableRequestsAsync(string agentId);
    Task<bool> AcceptDeliveryRequestAsync(string agentId, string parcelId);

    Task<IReadOnlyList<Parcel>> GetAgentDeliveriesAsync(string agentId);
    Task<AgentDeliveryManagementViewModel> GetAgentDeliveryDetailAsync(string agentId, string parcelIdOrTrackingId);

    Task<bool> MarkPickedUpAsync(string agentId, string parcelId, string? notes = null);
    Task<bool> MarkInTransitAsync(string agentId, string parcelId, string? notes = null);
    Task<bool> MarkOutForDeliveryAsync(string agentId, string parcelId, string? notes = null);
    Task<bool> ConfirmDeliveryAsync(string agentId, ConfirmDeliveryViewModel model);
    Task<bool> ReportDeliveryFailureAsync(string agentId, ReportFailureViewModel model);
}
