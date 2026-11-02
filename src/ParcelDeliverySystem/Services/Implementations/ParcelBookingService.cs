using Microsoft.Extensions.Logging;
using ParcelDeliverySystem.Models.DTOs.Pricing;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Parcels;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Implements the end-to-end parcel booking pipeline with address resolution,
/// Geoapify routing, dynamic charge calculation, and status history initialization.
/// </summary>
public class ParcelBookingService : IParcelBookingService
{
    private readonly IParcelRepository _parcelRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ITrackingIdGenerator _trackingIdGenerator;
    private readonly IDeliveryChargeService _deliveryChargeService;
    private readonly IRoutingService _routingService;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<ParcelBookingService> _logger;

    public ParcelBookingService(
        IParcelRepository parcelRepository,
        IAddressRepository addressRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        ITrackingIdGenerator trackingIdGenerator,
        IDeliveryChargeService deliveryChargeService,
        IRoutingService routingService,
        IGeocodingService geocodingService,
        ILogger<ParcelBookingService> logger)
    {
        _parcelRepository = parcelRepository;
        _addressRepository = addressRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _trackingIdGenerator = trackingIdGenerator;
        _deliveryChargeService = deliveryChargeService;
        _routingService = routingService;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<Parcel> BookParcelAsync(BookParcelViewModel model, string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new UnauthorizedAccessException("Customer ID cannot be empty.");

        var customer = await _userRepository.GetByIdAsync(customerId);
        if (customer == null || !customer.IsActive)
            throw new InvalidOperationException("Customer account is invalid or deactivated.");

        // 1. Resolve Pickup Address
        var pickupAddress = await ResolvePickupAddressAsync(model, customerId, customer);

        // 2. Resolve Delivery Address
        var deliveryAddress = await ResolveDeliveryAddressAsync(model, customerId);

        // 3. Compute Waypoint Route & Distance via Geoapify / Resilient Haversine Fallback
        var route = await _routingService.CalculateRouteAsync(
            pickupAddress.Latitude,
            pickupAddress.Longitude,
            deliveryAddress.Latitude,
            deliveryAddress.Longitude
        );

        // 4. Calculate Delivery Charges
        var chargeBreakdown = _deliveryChargeService.CalculateCharge(
            route.DistanceKm,
            model.Weight,
            model.Length,
            model.Width,
            model.Height,
            model.DeliveryType
        );

        // 5. Generate Unique Customer-Friendly Tracking ID
        var trackingId = await _trackingIdGenerator.GenerateTrackingIdAsync();

        // 6. Assemble the Parcel Entity Document
        var parcel = new Parcel
        {
            TrackingId = trackingId,
            CustomerId = customerId,
            ParcelType = model.ParcelType,
            Weight = model.Weight,
            Length = model.Length,
            Width = model.Width,
            Height = model.Height,
            Description = string.IsNullOrWhiteSpace(model.Description) ? $"{model.ParcelType} Shipment" : model.Description.Trim(),
            SpecialInstructions = model.SpecialInstructions?.Trim(),
            
            PickupAddressId = !string.IsNullOrWhiteSpace(pickupAddress.Id) && MongoDB.Bson.ObjectId.TryParse(pickupAddress.Id, out _) ? pickupAddress.Id : null,
            DeliveryAddressId = !string.IsNullOrWhiteSpace(deliveryAddress.Id) && MongoDB.Bson.ObjectId.TryParse(deliveryAddress.Id, out _) ? deliveryAddress.Id : null,
            PickupAddress = pickupAddress,
            DeliveryAddress = deliveryAddress,
            PickupLatitude = pickupAddress.Latitude,
            PickupLongitude = pickupAddress.Longitude,
            DeliveryLatitude = deliveryAddress.Latitude,
            DeliveryLongitude = deliveryAddress.Longitude,
            
            Distance = route.DistanceKm,
            EstimatedDurationMinutes = route.DurationMinutes,
            EstimatedDeliveryTime = chargeBreakdown.EstimatedDeliveryTime,
            DeliveryType = model.DeliveryType,
            DeliveryCharge = chargeBreakdown.TotalCharge,
            PaymentStatus = PaymentStatus.Pending,
            Status = ParcelStatus.Booked,
            BookingDate = DateTime.UtcNow,
            RouteGeometryJson = route.GeometryGeoJson,
            
            StatusHistory = new List<ParcelStatusHistory>
            {
                new()
                {
                    Status = ParcelStatus.Booked,
                    Timestamp = DateTime.UtcNow,
                    Location = pickupAddress.FormattedAddress,
                    Latitude = pickupAddress.Latitude,
                    Longitude = pickupAddress.Longitude,
                    Notes = $"Parcel successfully booked. Service tier: {model.DeliveryType}. Estimated delivery: {chargeBreakdown.EstimatedDeliveryDisplay}.",
                    UpdatedByUserId = customerId,
                    UpdatedByRole = "Customer"
                }
            }
        };

        // 7. Save Parcel Document in MongoDB Atlas
        var createdParcel = await _parcelRepository.CreateAsync(parcel);

        // 8. Create In-App Notification for Customer
        var notification = new Notification
        {
            UserId = customerId,
            Title = $"Shipment Booked: {trackingId}",
            Message = $"Your parcel to {deliveryAddress.RecipientName} has been booked. Tracking ID: {trackingId}. Total charge: ${chargeBreakdown.TotalCharge:F2}.",
            Type = NotificationType.BookingConfirmation,
            ReferenceId = trackingId,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepository.CreateAsync(notification);

        _logger.LogInformation("Parcel successfully booked. TrackingId: {TrackingId}, Customer: {Customer}", trackingId, customer.Email);
        return createdParcel;
    }

    public async Task<DeliveryChargeBreakdownDto> CalculateQuoteAsync(BookingQuoteRequestDto request)
    {
        var pickupLat = request.PickupLat;
        var pickupLon = request.PickupLon;
        var deliveryLat = request.DeliveryLat;
        var deliveryLon = request.DeliveryLon;

        // Geocode addresses if coordinates are zero
        if ((Math.Abs(pickupLat) < 0.0001 || Math.Abs(pickupLon) < 0.0001) && !string.IsNullOrWhiteSpace(request.PickupAddressText))
        {
            var geo = await _geocodingService.GeocodeAddressAsync(request.PickupAddressText);
            if (geo != null) { pickupLat = geo.Latitude; pickupLon = geo.Longitude; }
        }

        if ((Math.Abs(deliveryLat) < 0.0001 || Math.Abs(deliveryLon) < 0.0001) && !string.IsNullOrWhiteSpace(request.DeliveryAddressText))
        {
            var geo = await _geocodingService.GeocodeAddressAsync(request.DeliveryAddressText);
            if (geo != null) { deliveryLat = geo.Latitude; deliveryLon = geo.Longitude; }
        }

        var route = await _routingService.CalculateRouteAsync(pickupLat, pickupLon, deliveryLat, deliveryLon);

        return _deliveryChargeService.CalculateCharge(
            route.DistanceKm,
            request.Weight,
            request.Length,
            request.Width,
            request.Height,
            request.DeliveryType
        );
    }

    public async Task<Parcel?> GetParcelDetailsAsync(string trackingId, string? requestedByUserId = null, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(trackingId)) return null;

        var parcel = await _parcelRepository.GetByTrackingIdAsync(trackingId.Trim());
        if (parcel == null) return null;

        // Customer authorization rule: customer can only view their own parcel
        if (!string.IsNullOrEmpty(requestedByUserId) && !isAdmin)
        {
            if (parcel.CustomerId != requestedByUserId && parcel.AssignedAgentId != requestedByUserId)
            {
                _logger.LogWarning("Unauthorized parcel details access attempt: TrackingId {TrackingId} by User {UserId}", trackingId, requestedByUserId);
                return null;
            }
        }

        return parcel;
    }

    public async Task<IReadOnlyList<Parcel>> GetCustomerParcelsAsync(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId)) return Array.Empty<Parcel>();
        return await _parcelRepository.GetByCustomerIdAsync(customerId);
    }

    public async Task<bool> CancelBookingAsync(string trackingId, string customerId)
    {
        var parcel = await _parcelRepository.GetByTrackingIdAsync(trackingId);
        if (parcel == null || parcel.CustomerId != customerId)
        {
            _logger.LogWarning("Unauthorized cancel attempt: TrackingId {TrackingId} by User {UserId}", trackingId, customerId);
            return false;
        }

        // Only allow cancellation if parcel has not been picked up yet (Booked, PendingPickup, AgentAssigned)
        if (parcel.Status != ParcelStatus.Booked && 
            parcel.Status != ParcelStatus.PendingPickup && 
            parcel.Status != ParcelStatus.AgentAssigned)
        {
            _logger.LogWarning("Cancel rejected: TrackingId {TrackingId} is in status {Status}", trackingId, parcel.Status);
            return false;
        }

        var historyEntry = new ParcelStatusHistory
        {
            Status = ParcelStatus.Cancelled,
            Timestamp = DateTime.UtcNow,
            Location = parcel.PickupAddress?.FormattedAddress,
            Notes = "Booking cancelled by customer.",
            UpdatedByUserId = customerId,
            UpdatedByRole = "Customer"
        };

        var updated = await _parcelRepository.UpdateStatusAsync(parcel.Id, ParcelStatus.Cancelled, historyEntry);
        if (updated)
        {
            if (!string.IsNullOrEmpty(parcel.AssignedAgentId))
            {
                await _userRepository.UpdateAvailabilityStatusAsync(parcel.AssignedAgentId, AgentAvailabilityStatus.Available);
                await _notificationRepository.CreateAsync(new Notification
                {
                    UserId = parcel.AssignedAgentId,
                    Title = "Pickup Assignment Cancelled",
                    Message = $"The customer cancelled parcel shipment #{parcel.TrackingId}. Your status is now reset to Available.",
                    Type = NotificationType.Alert,
                    ReferenceId = parcel.Id
                });
            }

            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = customerId,
                Title = "Shipment Cancelled",
                Message = $"Your booking for parcel #{parcel.TrackingId} has been successfully cancelled.",
                Type = NotificationType.StatusUpdate,
                ReferenceId = parcel.Id
            });
        }

        return updated;
    }

    private async Task<Address> ResolvePickupAddressAsync(BookParcelViewModel model, string customerId, User customer)
    {
        if (model.PickupMode == "saved" && !string.IsNullOrWhiteSpace(model.SelectedPickupAddressId))
        {
            var saved = await _addressRepository.GetByIdAsync(model.SelectedPickupAddressId);
            if (saved != null && saved.UserId == customerId)
            {
                return saved;
            }
        }

        // Fallback or new pickup address
        var street = !string.IsNullOrWhiteSpace(model.NewPickupStreet) 
            ? model.NewPickupStreet.Trim() 
            : (model.NewPickupLatitude != 0 ? $"Pin Location ({model.NewPickupLatitude:F4}, {model.NewPickupLongitude:F4})" : "Pickup Origin");

        var newPickup = new Address
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = customerId,
            Label = "Pickup Location",
            RecipientName = string.IsNullOrWhiteSpace(model.NewPickupSenderName) ? customer.FullName : model.NewPickupSenderName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.NewPickupPhone) ? customer.PhoneNumber : model.NewPickupPhone.Trim(),
            StreetAddress = street,
            City = string.IsNullOrWhiteSpace(model.NewPickupCity) ? "Local Area" : model.NewPickupCity.Trim(),
            State = string.IsNullOrWhiteSpace(model.NewPickupState) ? "Region" : model.NewPickupState.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(model.NewPickupPostalCode) ? "00000" : model.NewPickupPostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(model.NewPickupCountry) ? "Default" : model.NewPickupCountry.Trim(),
            Latitude = model.NewPickupLatitude,
            Longitude = model.NewPickupLongitude,
            FormattedAddress = $"{street}, {model.NewPickupCity}, {model.NewPickupState} {model.NewPickupPostalCode}".Trim(',', ' ')
        };

        if (Math.Abs(newPickup.Latitude) < 0.0001 || Math.Abs(newPickup.Longitude) < 0.0001)
        {
            var geo = await _geocodingService.GeocodeAddressAsync(newPickup.FormattedAddress);
            if (geo != null)
            {
                newPickup.Latitude = geo.Latitude;
                newPickup.Longitude = geo.Longitude;
            }
            else
            {
                newPickup.Latitude = 44.0462;
                newPickup.Longitude = -123.0220;
            }
        }

        if (model.SaveNewPickupToAddressBook)
        {
            await _addressRepository.CreateAsync(newPickup);
        }

        return newPickup;
    }

    private async Task<Address> ResolveDeliveryAddressAsync(BookParcelViewModel model, string customerId)
    {
        if (model.DeliveryMode == "saved" && !string.IsNullOrWhiteSpace(model.SelectedDeliveryAddressId))
        {
            var saved = await _addressRepository.GetByIdAsync(model.SelectedDeliveryAddressId);
            if (saved != null && saved.UserId == customerId)
            {
                return saved;
            }
        }

        // New delivery destination
        var street = !string.IsNullOrWhiteSpace(model.NewDeliveryStreet) 
            ? model.NewDeliveryStreet.Trim() 
            : (model.NewDeliveryLatitude != 0 ? $"Pin Location ({model.NewDeliveryLatitude:F4}, {model.NewDeliveryLongitude:F4})" : "Delivery Destination");

        var newDelivery = new Address
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = customerId,
            Label = $"Destination ({model.RecipientName})",
            RecipientName = string.IsNullOrWhiteSpace(model.RecipientName) ? "Recipient" : model.RecipientName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.RecipientPhone) ? "+1 000-000-0000" : model.RecipientPhone.Trim(),
            StreetAddress = street,
            City = string.IsNullOrWhiteSpace(model.NewDeliveryCity) ? "Local Area" : model.NewDeliveryCity.Trim(),
            State = string.IsNullOrWhiteSpace(model.NewDeliveryState) ? "Region" : model.NewDeliveryState.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(model.NewDeliveryPostalCode) ? "00000" : model.NewDeliveryPostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(model.NewDeliveryCountry) ? "Default" : model.NewDeliveryCountry.Trim(),
            Latitude = model.NewDeliveryLatitude,
            Longitude = model.NewDeliveryLongitude,
            FormattedAddress = $"{street}, {model.NewDeliveryCity}, {model.NewDeliveryState} {model.NewDeliveryPostalCode}".Trim(',', ' ')
        };

        if (Math.Abs(newDelivery.Latitude) < 0.0001 || Math.Abs(newDelivery.Longitude) < 0.0001)
        {
            var geo = await _geocodingService.GeocodeAddressAsync(newDelivery.FormattedAddress);
            if (geo != null)
            {
                newDelivery.Latitude = geo.Latitude;
                newDelivery.Longitude = geo.Longitude;
            }
            else
            {
                newDelivery.Latitude = 45.5152;
                newDelivery.Longitude = -122.6784;
            }
        }

        if (model.SaveNewDeliveryToAddressBook)
        {
            await _addressRepository.CreateAsync(newDelivery);
        }

        return newDelivery;
    }
}
