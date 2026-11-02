using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Parcels;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Handles customer parcel booking wizard, quote estimations, parcel history, and parcel details.
/// Protected by [Authorize(Roles = "Customer,Admin")].
/// </summary>
[Authorize(Roles = "Customer,Admin")]
public class ParcelController : Controller
{
    private readonly IParcelBookingService _bookingService;
    private readonly IAddressService _addressService;
    private readonly IUserService _userService;
    private readonly ILogger<ParcelController> _logger;

    public ParcelController(
        IParcelBookingService bookingService,
        IAddressService addressService,
        IUserService userService,
        ILogger<ParcelController> logger)
    {
        _bookingService = bookingService;
        _addressService = addressService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Book()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var user = await _userService.GetUserByIdAsync(customerId);
        var savedAddresses = await _addressService.GetAddressesByUserIdAsync(customerId);
        var defaultAddress = savedAddresses.FirstOrDefault(a => a.IsDefault) ?? savedAddresses.FirstOrDefault();

        var model = new BookParcelViewModel
        {
            SavedAddresses = savedAddresses,
            SelectedPickupAddressId = defaultAddress?.Id,
            PickupMode = savedAddresses.Any() ? "saved" : "new",
            NewPickupSenderName = user?.FullName ?? string.Empty,
            NewPickupPhone = user?.PhoneNumber ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookParcelViewModel model)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            model.SavedAddresses = await _addressService.GetAddressesByUserIdAsync(customerId);
            return View(model);
        }

        try
        {
            var createdParcel = await _bookingService.BookParcelAsync(model, customerId);
            TempData["SuccessMessage"] = $"Parcel booked successfully! Your tracking code is {createdParcel.TrackingId}.";
            return RedirectToAction(nameof(Details), new { id = createdParcel.TrackingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while booking parcel for Customer {CustomerId}", customerId);
            ModelState.AddModelError(string.Empty, $"Booking failed: {ex.Message}");
            model.SavedAddresses = await _addressService.GetAddressesByUserIdAsync(customerId);
            return View(model);
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CalculateQuote([FromBody] BookingQuoteRequestDto request)
    {
        if (request == null) return BadRequest(new { message = "Invalid quote parameters." });

        try
        {
            var quote = await _bookingService.CalculateQuoteAsync(request);
            return Json(new { success = true, quote });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate live booking quote.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyParcels()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var parcels = await _bookingService.GetCustomerParcelsAsync(customerId);
        return View(parcels);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var parcel = await _bookingService.GetParcelDetailsAsync(id, customerId, isAdmin);
        if (parcel == null)
        {
            TempData["ErrorMessage"] = "Shipment record not found or access is restricted.";
            return RedirectToAction(nameof(MyParcels));
        }

        var customer = await _userService.GetUserByIdAsync(parcel.CustomerId);
        var agent = !string.IsNullOrEmpty(parcel.AssignedAgentId)
            ? await _userService.GetUserByIdAsync(parcel.AssignedAgentId)
            : null;

        var viewModel = new ParcelDetailsViewModel
        {
            Parcel = parcel,
            Customer = customer,
            AssignedAgent = agent,
            CanCancel = parcel.Status == ParcelStatus.Booked || parcel.Status == ParcelStatus.PendingPickup || parcel.Status == ParcelStatus.AgentAssigned,
            IsOwnerOrAdmin = true
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string trackingId)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var success = await _bookingService.CancelBookingAsync(trackingId, customerId);
        if (success)
        {
            TempData["SuccessMessage"] = $"Shipment {trackingId} has been cancelled.";
        }
        else
        {
            TempData["ErrorMessage"] = "Unable to cancel shipment. Parcels in transit or already delivered cannot be cancelled.";
        }

        return RedirectToAction(nameof(Details), new { id = trackingId });
    }
}
