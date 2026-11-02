using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParcelDeliverySystem.Models.Common;
using ParcelDeliverySystem.Models.Enums;

namespace ParcelDeliverySystem.Models.Entities;

/// <summary>
/// In-app notification for users (Customers, Agents, Admins).
/// </summary>
public class Notification : BaseEntity
{
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public NotificationType Type { get; set; } = NotificationType.General;

    [BsonElement("referenceId")]
    public string? ReferenceId { get; set; } // TrackingId or ParcelId

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("readAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ReadAt { get; set; }
}
