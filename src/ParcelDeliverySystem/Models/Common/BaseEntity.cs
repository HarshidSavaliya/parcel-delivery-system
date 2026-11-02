using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ParcelDeliverySystem.Models.Common;

/// <summary>
/// Base class for all MongoDB documents in the system.
/// Uses string Id mapped to MongoDB internal ObjectId with auto-generated unique ID defaults.
/// </summary>
public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; set; }
}
