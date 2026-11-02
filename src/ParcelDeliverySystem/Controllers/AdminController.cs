using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Admin;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Controller for Central System Administrators governing telemetry dashboards, customer/agent provisioning,
/// multi-parameter shipment search, and operational fleet reports.
/// Protected by [Authorize(Roles = "Admin")].
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminService adminService,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var viewModel = await _adminService.GetDashboardMetricsAsync();
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? search = null)
    {
        ViewBag.SearchTerm = search;
        var customers = await _adminService.GetCustomersAsync(search);
        return View(customers);
    }

    [HttpGet]
    public async Task<IActionResult> CustomerDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var details = await _adminService.GetCustomerDetailsAsync(id);
        if (details == null) return NotFound();

        return View(details);
    }

    [HttpGet]
    public async Task<IActionResult> Agents(string? search = null, AgentAvailabilityStatus? status = null)
    {
        ViewBag.SearchTerm = search;
        ViewBag.StatusFilter = status;

        var agents = await _adminService.GetAgentsAsync(search, status);
        return View(agents);
    }

    [HttpGet]
    public async Task<IActionResult> AgentDetails(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var details = await _adminService.GetAgentDetailsAsync(id);
        if (details == null) return NotFound();

        return View(details);
    }

    [HttpGet]
    public IActionResult CreateAgent()
    {
        return View(new CreateAgentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAgent(CreateAgentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _adminService.CreateAgentAsync(model);
            TempData["SuccessMessage"] = $"Delivery Agent account '{model.FullName}' created successfully.";
            return RedirectToAction(nameof(Agents));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Email", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery agent account.");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while provisioning the agent account.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditAgent(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var details = await _adminService.GetAgentDetailsAsync(id);
        if (details == null) return NotFound();

        var model = new EditAgentViewModel
        {
            Id = details.Agent.Id,
            FullName = details.Agent.FullName,
            Email = details.Agent.Email,
            PhoneNumber = details.Agent.PhoneNumber,
            Address = details.Agent.Address,
            VehicleType = details.Agent.VehicleType,
            VehiclePlateNumber = details.Agent.VehiclePlateNumber,
            DrivingLicenseNumber = details.Agent.DrivingLicenseNumber,
            AvailabilityStatus = details.Agent.AvailabilityStatus,
            IsActive = details.Agent.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAgent(EditAgentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _adminService.UpdateAgentAsync(model.Id, model);
        if (success)
        {
            TempData["SuccessMessage"] = $"Agent profile for '{model.FullName}' updated successfully.";
            return RedirectToAction(nameof(Agents));
        }

        TempData["ErrorMessage"] = "Failed to update delivery agent profile.";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string userId, string? returnAction = "Users")
    {
        var success = await _adminService.ToggleUserStatusAsync(userId);
        if (success)
        {
            TempData["SuccessMessage"] = "User status updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Unable to update user status.";
        }

        return RedirectToAction(returnAction ?? "Users");
    }

    [HttpGet]
    public async Task<IActionResult> Parcels(ParcelSearchFilterViewModel filter)
    {
        var result = await _adminService.SearchParcelsAsync(filter);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Reports(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var reportData = await _adminService.GetAdminReportDataAsync(dateFrom, dateTo);
        return View(reportData);
    }
}
