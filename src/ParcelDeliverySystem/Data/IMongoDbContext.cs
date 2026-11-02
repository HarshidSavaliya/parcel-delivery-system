using MongoDB.Driver;
using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Data;

/// <summary>
/// Abstraction for MongoDB Atlas database context.
/// Provides typed access to official MongoDB driver collections without coupling to EF Core.
/// </summary>
public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoClient Client { get; }

    IMongoCollection<User> Users { get; }
    IMongoCollection<Address> Addresses { get; }
    IMongoCollection<Parcel> Parcels { get; }
    IMongoCollection<Notification> Notifications { get; }
    IMongoCollection<DeliveryRecord> DeliveryRecords { get; }
    IMongoCollection<AgentAssignment> AgentAssignments { get; }

    IMongoCollection<T> GetCollection<T>(string name);
}
