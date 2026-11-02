using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.ViewModels.Parcels;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Public and authenticated shipment tracking controller.
/// </summary>
public class TrackingController : Controller
{
    private readonly IParcelRepository _parcelRepository;

    public TrackingController(IParcelRepository parcelRepository)
    {
        _parcelRepository = parcelRepository;
    }

    [HttpGet]
    public IActionResult Index(string? trackingId)
    {
        return RedirectToAction(nameof(Track), new { trackingId });
    }

    [HttpGet]
    public async Task<IActionResult> Track(string? trackingId)
    {
        ViewBag.TrackingQuery = trackingId?.Trim();

        if (string.IsNullOrWhiteSpace(trackingId))
        {
            return View((ParcelDetailsViewModel?)null);
        }

        var parcel = await _parcelRepository.GetByTrackingIdAsync(trackingId.Trim());
        if (parcel == null)
        {
            return View((ParcelDetailsViewModel?)null);
        }

        var viewModel = new ParcelDetailsViewModel
        {
            Parcel = parcel,
            CanCancel = false,
            IsOwnerOrAdmin = false
        };

        return View(viewModel);
    }
}
