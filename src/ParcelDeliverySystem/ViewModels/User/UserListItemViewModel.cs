using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Users;

/// <summary>
/// View model for listing users in the Admin control center.
/// </summary>
public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public AgentAvailabilityStatus? AvailabilityStatus { get; set; }
    public string? VehicleType { get; set; }
    public double Rating { get; set; }
    public int CompletedDeliveriesCount { get; set; }
}
