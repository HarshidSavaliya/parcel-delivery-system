using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Models;
using ParcelDeliverySystem.Repositories.Interfaces;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Public home and informational controller with hero showcase and tracking lookup.
/// </summary>
public class HomeController : Controller
{
    private readonly IParcelRepository _parcelRepository;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IParcelRepository parcelRepository, ILogger<HomeController> logger)
    {
        _parcelRepository = parcelRepository;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // If user is already logged in, redirect them to their respective role dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("DeliveryAgent"))
                return RedirectToAction("Dashboard", "DeliveryAgent");
            if (User.IsInRole("Customer"))
                return RedirectToAction("Dashboard", "Customer");
        }

        return View();
    }

    [HttpGet]
    public IActionResult Track(string? trackingId)
    {
        if (string.IsNullOrWhiteSpace(trackingId))
        {
            return RedirectToAction("Track", "Tracking");
        }

        return RedirectToAction("Track", "Tracking", new { trackingId = trackingId.Trim() });
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null, string? requestId = null)
    {
        var model = new ErrorViewModel
        {
            RequestId = requestId ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode ?? 500
        };

        if (model.StatusCode == 404)
        {
            model.Title = "Page or Resource Not Found";
            model.Message = "The page or parcel record you are looking for might have been removed, had its name changed, or is temporarily unavailable.";
        }
        else if (model.StatusCode == 403)
        {
            model.Title = "Access Forbidden";
            model.Message = "You do not have the required permissions to view this resource.";
        }
        else
        {
            model.Title = "Something Went Wrong";
            model.Message = "An unexpected error occurred while processing your request. Our system engineers have been alerted.";
        }

        return View(model);
    }
}
