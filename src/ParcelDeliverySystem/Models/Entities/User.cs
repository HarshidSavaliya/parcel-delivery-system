using MongoDB.Bson.Serialization.Attributes;
using ParcelDeliverySystem.Models.Common;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Models.Entities;

/// <summary>
/// Represents a user document in the 'Users' MongoDB collection.
/// Supports Customers, Delivery Agents, and Administrators.
/// </summary>
public class User : BaseEntity
{
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public UserRole Role { get; set; } = UserRole.Customer;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("lastLoginAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastLoginAt { get; set; }

    // --- Delivery Agent Specific Fields ---
    [BsonElement("availabilityStatus")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public AgentAvailabilityStatus AvailabilityStatus { get; set; } = AgentAvailabilityStatus.Offline;

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("vehicleType")]
    public string? VehicleType { get; set; }

    [BsonElement("vehiclePlateNumber")]
    public string? VehiclePlateNumber { get; set; }

    [BsonElement("drivingLicenseNumber")]
    public string? DrivingLicenseNumber { get; set; }

    [BsonElement("currentLatitude")]
    public double? CurrentLatitude { get; set; }

    [BsonElement("currentLongitude")]
    public double? CurrentLongitude { get; set; }

    [BsonElement("completedDeliveriesCount")]
    public int CompletedDeliveriesCount { get; set; } = 0;

    [BsonElement("rating")]
    public double Rating { get; set; } = 5.0;
}
