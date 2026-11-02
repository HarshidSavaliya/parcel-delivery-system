using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParcelDeliverySystem.Models.Common;

namespace ParcelDeliverySystem.Models.Entities;

/// <summary>
/// Represents a saved delivery/pickup address document or subdocument.
/// Stored in 'Addresses' collection and embedded where appropriate.
/// </summary>
public class Address : BaseEntity
{
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = "Home"; // e.g. "Home", "Work", "Warehouse"

    [BsonElement("recipientName")]
    public string RecipientName { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [BsonElement("streetAddress")]
    public string StreetAddress { get; set; } = string.Empty;

    [BsonElement("unitOrApt")]
    public string? UnitOrApt { get; set; }

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = "United States";

    [BsonElement("formattedAddress")]
    public string FormattedAddress { get; set; } = string.Empty;

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("isDefault")]
    public bool IsDefault { get; set; } = false;
}
