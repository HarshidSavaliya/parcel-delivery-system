using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Models.Entities;

/// <summary>
/// Status audit log entry embedded within a Parcel or stored in ParcelStatusHistory.
/// </summary>
public class ParcelStatusHistory
{
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ParcelStatus Status { get; set; }

    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("latitude")]
    public double? Latitude { get; set; }

    [BsonElement("longitude")]
    public double? Longitude { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("updatedByUserId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UpdatedByUserId { get; set; }

    [BsonElement("updatedByRole")]
    public string? UpdatedByRole { get; set; }
}
