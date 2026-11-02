using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParcelDeliverySystem.Models.Common;

namespace ParcelDeliverySystem.Models.Entities;

/// <summary>
/// Archival and auditing proof of delivery document.
/// </summary>
public class DeliveryRecord : BaseEntity
{
    [BsonElement("parcelId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ParcelId { get; set; } = string.Empty;

    [BsonElement("trackingId")]
    public string TrackingId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("agentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AgentId { get; set; } = string.Empty;

    [BsonElement("receivedBy")]
    public string ReceivedBy { get; set; } = string.Empty;

    [BsonElement("signatureBase64")]
    public string? SignatureBase64 { get; set; }

    [BsonElement("deliveryNotes")]
    public string? DeliveryNotes { get; set; }

    [BsonElement("photoProofUrl")]
    public string? PhotoProofUrl { get; set; }

    [BsonElement("completedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
