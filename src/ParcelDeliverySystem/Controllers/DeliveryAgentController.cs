using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Agent;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Controller for authenticated Delivery Agents to manage active deliveries, proximity requests,
/// lifecycle progression (Picked Up, In Transit, Out for Delivery, Delivered, Failed), and availability.
/// Protected by [Authorize(Roles = "DeliveryAgent,Admin")].
/// </summary>
[Authorize(Roles = "DeliveryAgent,Admin")]
public class DeliveryAgentController : Controller
{
    private readonly IDeliveryAgentService _deliveryAgentService;
    private readonly IUserService _userService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<DeliveryAgentController> _logger;

    public DeliveryAgentController(
        IDeliveryAgentService deliveryAgentService,
        IUserService userService,
        INotificationRepository notificationRepository,
        ILogger<DeliveryAgentController> logger)
    {
        _deliveryAgentService = deliveryAgentService;
        _userService = userService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return RedirectToAction("Login", "Account");
        }

        var viewModel = await _userService.GetAgentDashboardDataAsync(agentId);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return RedirectToAction("Login", "Account");
        }

        var profile = await _deliveryAgentService.GetAgentProfileAsync(agentId);
        if (profile == null)
        {
            TempData["ErrorMessage"] = "Agent profile not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(AgentProfileViewModel model)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await _deliveryAgentService.UpdateAgentProfileAsync(agentId, model);
        if (updated)
        {
            TempData["SuccessMessage"] = "Your delivery partner profile has been successfully updated.";
            return RedirectToAction(nameof(Profile));
        }

        TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability(AgentAvailabilityStatus status)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return Json(new { success = false, message = "Not authenticated" });
        }

        var success = await _deliveryAgentService.UpdateAvailabilityStatusAsync(agentId, status);
        if (success)
        {
            TempData["SuccessMessage"] = $"Your availability status is now set to '{status}'.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update availability status.";
        }

        var returnUrl = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> AvailableRequests()
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return RedirectToAction("Login", "Account");
        }

        var agentProfile = await _deliveryAgentService.GetAgentProfileAsync(agentId);
        ViewBag.AgentAvailability = agentProfile?.AvailabilityStatus ?? AgentAvailabilityStatus.Offline;
        ViewBag.AgentLatitude = agentProfile?.CurrentLatitude ?? 12.9716;
        ViewBag.AgentLongitude = agentProfile?.CurrentLongitude ?? 77.5946;

        var availableRequests = await _deliveryAgentService.GetAvailableRequestsAsync(agentId);
        return View(availableRequests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptRequest(string parcelId)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(parcelId))
        {
            TempData["ErrorMessage"] = "Invalid parcel identifier.";
            return RedirectToAction(nameof(AvailableRequests));
        }

        try
        {
            await _deliveryAgentService.AcceptDeliveryRequestAsync(agentId, parcelId);
            TempData["SuccessMessage"] = "Delivery request accepted! You have been assigned to this shipment.";
            return RedirectToAction(nameof(ManageDelivery), new { id = parcelId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to accept parcel {ParcelId} by Agent {AgentId}", parcelId, agentId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(AvailableRequests));
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyDeliveries()
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return RedirectToAction("Login", "Account");
        }

        var parcels = await _deliveryAgentService.GetAgentDeliveriesAsync(agentId);
        return View(parcels);
    }

    [HttpGet]
    public async Task<IActionResult> ManageDelivery(string id)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        try
        {
            var viewModel = await _deliveryAgentService.GetAgentDeliveryDetailAsync(agentId, id);
            return View(viewModel);
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(MyDeliveries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing delivery {ParcelId}", id);
            TempData["ErrorMessage"] = "Shipment not found or unavailable.";
            return RedirectToAction(nameof(MyDeliveries));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPickedUp(string parcelId, string? notes)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId)) return RedirectToAction("Login", "Account");

        try
        {
            await _deliveryAgentService.MarkPickedUpAsync(agentId, parcelId, notes);
            TempData["SuccessMessage"] = "Parcel marked as Picked Up successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(ManageDelivery), new { id = parcelId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkInTransit(string parcelId, string? notes)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId)) return RedirectToAction("Login", "Account");

        try
        {
            await _deliveryAgentService.MarkInTransitAsync(agentId, parcelId, notes);
            TempData["SuccessMessage"] = "Parcel status transitioned to In Transit.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(ManageDelivery), new { id = parcelId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkOutForDelivery(string parcelId, string? notes)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId)) return RedirectToAction("Login", "Account");

        try
        {
            await _deliveryAgentService.MarkOutForDeliveryAsync(agentId, parcelId, notes);
            TempData["SuccessMessage"] = "Parcel marked as Out For Delivery.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(ManageDelivery), new { id = parcelId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelivery(ConfirmDeliveryViewModel model)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId)) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fill in all required delivery confirmation details.";
            return RedirectToAction(nameof(ManageDelivery), new { id = model.ParcelId });
        }

        try
        {
            await _deliveryAgentService.ConfirmDeliveryAsync(agentId, model);
            TempData["SuccessMessage"] = "Shipment marked as Delivered! Proof of delivery recorded and your status is now Available.";
            return RedirectToAction(nameof(MyDeliveries));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(ManageDelivery), new { id = model.ParcelId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportDeliveryFailure(ReportFailureViewModel model)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId)) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please specify a valid reason and notes for delivery failure.";
            return RedirectToAction(nameof(ManageDelivery), new { id = model.ParcelId });
        }

        try
        {
            await _deliveryAgentService.ReportDeliveryFailureAsync(agentId, model);
            TempData["SuccessMessage"] = "Delivery failure recorded. Your status is now reset to Available.";
            return RedirectToAction(nameof(MyDeliveries));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(ManageDelivery), new { id = model.ParcelId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId)) return RedirectToAction("Login", "Account");

        var notifications = await _notificationRepository.GetByUserIdAsync(agentId, 50);
        await _notificationRepository.MarkAllAsReadAsync(agentId);
        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateRequestDto request)
    {
        var agentId = GetCurrentAgentId();
        if (string.IsNullOrEmpty(agentId)) return Unauthorized();

        var success = await _deliveryAgentService.UpdateLocationAsync(agentId, request.Latitude, request.Longitude);
        return Json(new { success });
    }

    private string GetCurrentAgentId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}

public class LocationUpdateRequestDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
