using MongoDB.Driver;
using ParcelDeliverySystem.Data;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Repositories.Implementations;

/// <summary>
/// MongoDB implementation of IAgentAssignmentRepository.
/// </summary>
public class AgentAssignmentRepository : BaseRepository<AgentAssignment>, IAgentAssignmentRepository
{
    public AgentAssignmentRepository(IMongoDbContext context) : base(context.AgentAssignments)
    {
    }

    public async Task<AgentAssignment?> GetActiveAssignmentForParcelAsync(string parcelId)
    {
        return await _collection.Find(a => 
            a.ParcelId == parcelId && 
            a.Status != "Completed" && 
            a.Status != "Failed" && 
            a.Status != "Cancelled"
        ).FirstOrDefaultAsync();
    }

    public async Task<AgentAssignment?> GetLatestAssignmentForParcelAsync(string parcelId)
    {
        return await _collection.Find(a => a.ParcelId == parcelId)
            .SortByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<AgentAssignment>> GetByAgentIdAsync(string agentId)
    {
        return await _collection.Find(a => a.AgentId == agentId)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AgentAssignment>> GetActiveByAgentIdAsync(string agentId)
    {
        return await _collection.Find(a => 
            a.AgentId == agentId && 
            a.Status != "Completed" && 
            a.Status != "Failed" && 
            a.Status != "Cancelled"
        ).SortByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<bool> HasActiveAssignmentAsync(string agentId)
    {
        return await _collection.Find(a => 
            a.AgentId == agentId && 
            a.Status != "Completed" && 
            a.Status != "Failed" && 
            a.Status != "Cancelled"
        ).AnyAsync();
    }
}
