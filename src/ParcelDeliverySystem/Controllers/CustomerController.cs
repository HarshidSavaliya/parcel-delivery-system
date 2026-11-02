using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Customer portal controller for managing personal profile, saved address book, and notifications.
/// Protected by [Authorize(Roles = "Customer")].
/// </summary>
[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    private readonly IUserService _userService;
    private readonly IAddressService _addressService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        IUserService userService,
        IAddressService addressService,
        INotificationRepository notificationRepository,
        ILogger<CustomerController> logger)
    {
        _userService = userService;
        _addressService = addressService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var viewModel = await _userService.GetCustomerDashboardDataAsync(customerId);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var profile = await _userService.GetUserProfileAsync(customerId);
        if (profile == null) return NotFound();

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId) || customerId != model.Id) return Forbid();

        if (!ModelState.IsValid) return View(model);

        var success = await _userService.UpdateProfileAsync(customerId, model);
        if (success)
        {
            TempData["SuccessMessage"] = "Profile details and contact number updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(string.Empty, "Unable to update profile. Please try again.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Addresses()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var addresses = await _addressService.GetAddressesByUserIdAsync(customerId);
        return View(addresses);
    }

    [HttpGet]
    public IActionResult CreateAddress()
    {
        var model = new Address
        {
            RecipientName = User.Identity?.Name ?? string.Empty,
            PhoneNumber = User.FindFirst("PhoneNumber")?.Value ?? string.Empty
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddress(Address model)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(model.StreetAddress) || string.IsNullOrWhiteSpace(model.City))
        {
            ModelState.AddModelError(string.Empty, "Street Address and City are required.");
            return View(model);
        }

        await _addressService.CreateAddressAsync(model, customerId);
        TempData["SuccessMessage"] = "New address saved to your address book.";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpGet]
    public async Task<IActionResult> EditAddress(string id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var address = await _addressService.GetAddressByIdAsync(id, customerId);
        if (address == null)
        {
            TempData["ErrorMessage"] = "Address not found or access is denied.";
            return RedirectToAction(nameof(Addresses));
        }

        return View(address);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(Address model)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(model.StreetAddress) || string.IsNullOrWhiteSpace(model.City))
        {
            ModelState.AddModelError(string.Empty, "Street Address and City are required.");
            return View(model);
        }

        var success = await _addressService.UpdateAddressAsync(model, customerId);
        if (success)
        {
            TempData["SuccessMessage"] = "Address updated successfully.";
            return RedirectToAction(nameof(Addresses));
        }

        ModelState.AddModelError(string.Empty, "Unable to update address.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(string id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var success = await _addressService.DeleteAddressAsync(id, customerId);
        if (success)
        {
            TempData["SuccessMessage"] = "Address removed from your address book.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete address.";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(string id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

        var success = await _addressService.SetDefaultAddressAsync(id, customerId);
        if (success)
        {
            TempData["SuccessMessage"] = "Default delivery address updated.";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpGet]
    public IActionResult BookParcel()
    {
        return RedirectToAction("Book", "Parcel");
    }

    [HttpGet]
    public IActionResult MyParcels()
    {
        return RedirectToAction("MyParcels", "Parcel");
    }

    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var notifications = await _notificationRepository.GetByUserIdAsync(customerId, 50);
        await _notificationRepository.MarkAllAsReadAsync(customerId);
        return View(notifications);
    }
}
