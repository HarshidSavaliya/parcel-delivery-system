using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.ViewModels.Admin;

/// <summary>
/// Aggregated reporting view model with date filters, shipment volume breakdown, and fleet performance metrics.
/// </summary>
public class AdminReportViewModel
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    // Shipment Volume Metrics
    public long TotalParcelsCount { get; set; }
    public long DeliveredParcelsCount { get; set; }
    public long PendingParcelsCount { get; set; }
    public long InTransitParcelsCount { get; set; }
    public long CancelledParcelsCount { get; set; }
    public long FailedDeliveriesCount { get; set; }

    // Financial Metrics
    public decimal TotalRevenueGenerated { get; set; }
    public decimal AverageRevenuePerParcel => TotalParcelsCount > 0
        ? Math.Round(TotalRevenueGenerated / TotalParcelsCount, 2)
        : 0;

    // Delivery Reliability
    public double FulfillmentSuccessRate => TotalParcelsCount > 0
        ? Math.Round(((double)DeliveredParcelsCount / TotalParcelsCount) * 100.0, 1)
        : 100.0;

    // Date-wise Breakdown
    public List<DateVolumeReportItem> DailyVolumeBreakdown { get; set; } = new();

    // Agent Fleet Performance
    public List<AgentPerformanceReportItemViewModel> AgentPerformanceList { get; set; } = new();
}

public class DateVolumeReportItem
{
    public DateTime Date { get; set; }
    public string FormattedDate => Date.ToString("MMM dd, yyyy");
    public int BookedCount { get; set; }
    public int DeliveredCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal Revenue { get; set; }
}
