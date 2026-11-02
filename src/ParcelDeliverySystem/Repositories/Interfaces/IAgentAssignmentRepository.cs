using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Repositories.Interfaces;

/// <summary>
/// Repository interface for managing agent assignments and delivery lifecycles.
/// </summary>
public interface IAgentAssignmentRepository : IRepository<AgentAssignment>
{
    Task<AgentAssignment?> GetActiveAssignmentForParcelAsync(string parcelId);
    Task<AgentAssignment?> GetLatestAssignmentForParcelAsync(string parcelId);
    Task<IReadOnlyList<AgentAssignment>> GetByAgentIdAsync(string agentId);
    Task<IReadOnlyList<AgentAssignment>> GetActiveByAgentIdAsync(string agentId);
    Task<bool> HasActiveAssignmentAsync(string agentId);
}
