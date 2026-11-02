using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.ViewModels.Admin;

/// <summary>
/// Metric model for delivery agent courier fleet performance in reports.
/// </summary>
public class AgentPerformanceReportItemViewModel
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public AgentAvailabilityStatus AvailabilityStatus { get; set; }
    public bool IsActive { get; set; }

    public int AssignedDeliveriesCount { get; set; }
    public int CompletedDeliveriesCount { get; set; }
    public int FailedDeliveriesCount { get; set; }
    public int ActiveDeliveriesCount { get; set; }

    public double SuccessRatePercentage => AssignedDeliveriesCount > 0
        ? Math.Round(((double)CompletedDeliveriesCount / AssignedDeliveriesCount) * 100.0, 1)
        : 100.0;

    public double Rating { get; set; } = 5.0;
}
