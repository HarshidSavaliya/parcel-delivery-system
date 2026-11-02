using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Repositories.Interfaces;

/// <summary>
/// Parcel repository interface for querying shipments, tracking IDs, and status transitions.
/// </summary>
public interface IParcelRepository : IRepository<Parcel>
{
    Task<Parcel?> GetByTrackingIdAsync(string trackingId);
    Task<IReadOnlyList<Parcel>> GetByCustomerIdAsync(string customerId);
    Task<IReadOnlyList<Parcel>> GetByAgentIdAsync(string agentId);
    Task<IReadOnlyList<Parcel>> GetAvailableParcelsForPickupAsync();
    Task<IReadOnlyList<Parcel>> GetRecentParcelsAsync(int limit = 10);
    Task<bool> UpdateStatusAsync(string parcelId, ParcelStatus newStatus, ParcelStatusHistory historyEntry);
    Task<bool> AssignAgentAsync(string parcelId, string agentId);
    Task<bool> AssignAgentSafelyAsync(string parcelId, string agentId, ParcelStatusHistory historyEntry);
    Task<long> CountByStatusAsync(ParcelStatus status);
}
