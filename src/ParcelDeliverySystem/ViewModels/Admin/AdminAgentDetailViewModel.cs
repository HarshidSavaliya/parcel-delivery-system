using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.ViewModels.Admin;

/// <summary>
/// View model for administrator inspecting detailed agent profile, live assignments, vehicle info, and delivery history.
/// </summary>
public class AdminAgentDetailViewModel
{
    public User Agent { get; set; } = new();
    public IReadOnlyList<Parcel> ActiveDeliveries { get; set; } = new List<Parcel>();
    public IReadOnlyList<Parcel> CompletedDeliveries { get; set; } = new List<Parcel>();
    public IReadOnlyList<AgentAssignment> RecentAssignments { get; set; } = new List<AgentAssignment>();
    public double SuccessRate { get; set; }
}
