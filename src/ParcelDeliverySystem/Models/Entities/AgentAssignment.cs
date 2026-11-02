using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParcelDeliverySystem.Models.Common;

namespace ParcelDeliverySystem.Models.Entities;

/// <summary>
/// Tracks the assignment and acceptance lifecycle between a Parcel and a Delivery Agent.
/// </summary>
public class AgentAssignment : BaseEntity
{
    [BsonElement("parcelId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ParcelId { get; set; } = string.Empty;

    [BsonElement("agentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AgentId { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "Assigned"; // Assigned, Accepted, PickedUp, InTransit, OutForDelivery, Completed, Failed, Cancelled

    [BsonElement("assignedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("acceptedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? AcceptedAt { get; set; }

    [BsonElement("pickupTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? PickupTime { get; set; }

    [BsonElement("deliveryTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeliveryTime { get; set; }

    [BsonElement("respondedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? RespondedAt { get; set; }

    [BsonElement("completedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("deliveredTo")]
    public string? DeliveredTo { get; set; }

    [BsonElement("signatureBase64")]
    public string? SignatureBase64 { get; set; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }
}
