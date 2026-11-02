namespace ParcelDeliverySystem.Models;

/// <summary>
/// Friendly error view model for global exception handler and error status pages.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; set; } = 500;
    public string Title { get; set; } = "An unexpected error occurred";
    public string Message { get; set; } = "We encountered an error processing your request. Please try again or contact support if the issue persists.";
}
