using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParcelDeliverySystem.Models.Common;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Models.Entities;

/// <summary>
/// Represents a parcel document in the 'Parcels' MongoDB collection.
/// Contains complete shipment telemetry, addresses, route coordinates, pricing, and history.
/// </summary>
public class Parcel : BaseEntity
{
    [BsonElement("trackingId")]
    public string TrackingId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("assignedAgentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedAgentId { get; set; }

    [BsonElement("parcelType")]
    public string ParcelType { get; set; } = "Standard Box"; // Document, Small Box, Medium Box, Large Package, Fragile

    [BsonElement("weight")]
    public double Weight { get; set; } = 1.0; // In kilograms

    [BsonElement("length")]
    public double Length { get; set; } = 10.0; // In cm

    [BsonElement("width")]
    public double Width { get; set; } = 10.0; // In cm

    [BsonElement("height")]
    public double Height { get; set; } = 10.0; // In cm

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("specialInstructions")]
    public string? SpecialInstructions { get; set; }

    [BsonElement("recipientEmail")]
    public string? RecipientEmail { get; set; }

    // --- Address Linkage & Coordinates ---
    [BsonElement("pickupAddressId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PickupAddressId { get; set; }

    [BsonElement("deliveryAddressId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? DeliveryAddressId { get; set; }

    [BsonElement("pickupAddress")]
    public Address PickupAddress { get; set; } = new();

    [BsonElement("deliveryAddress")]
    public Address DeliveryAddress { get; set; } = new();

    [BsonElement("pickupLatitude")]
    public double PickupLatitude { get; set; }

    [BsonElement("pickupLongitude")]
    public double PickupLongitude { get; set; }

    [BsonElement("deliveryLatitude")]
    public double DeliveryLatitude { get; set; }

    [BsonElement("deliveryLongitude")]
    public double DeliveryLongitude { get; set; }

    // --- Routing & Pricing ---
    [BsonElement("distance")]
    public double Distance { get; set; } // Distance in km

    [BsonElement("estimatedDurationMinutes")]
    public int EstimatedDurationMinutes { get; set; }

    [BsonElement("estimatedDeliveryTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? EstimatedDeliveryTime { get; set; }

    [BsonElement("deliveryType")]
    [BsonRepresentation(BsonType.String)]
    public DeliveryType DeliveryType { get; set; } = DeliveryType.Standard;

    [BsonElement("deliveryCharge")]
    public decimal DeliveryCharge { get; set; }

    [BsonElement("paymentStatus")]
    [BsonRepresentation(BsonType.String)]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ParcelStatus Status { get; set; } = ParcelStatus.Booked;

    [BsonElement("bookingDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    [BsonElement("pickedUpAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? PickedUpAt { get; set; }

    [BsonElement("deliveredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeliveredAt { get; set; }

    [BsonElement("routeGeometryJson")]
    public string? RouteGeometryJson { get; set; } // GeoJSON line string from Geoapify

    [BsonElement("statusHistory")]
    public List<ParcelStatusHistory> StatusHistory { get; set; } = new();

    // Backward compatibility helper properties
    [BsonIgnore]
    public double WeightKg => Weight;

    [BsonIgnore]
    public double DistanceKm => Distance;

    [BsonIgnore]
    public decimal Price => DeliveryCharge;

    [BsonIgnore]
    public string PackageType => ParcelType;

    [BsonIgnore]
    public string RecipientName => DeliveryAddress.RecipientName;

    [BsonIgnore]
    public string RecipientPhone => DeliveryAddress.PhoneNumber;
}
