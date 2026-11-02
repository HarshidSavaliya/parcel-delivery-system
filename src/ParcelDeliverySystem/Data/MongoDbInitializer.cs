using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Data;

/// <summary>
/// Initializes MongoDB collections and builds high-performance indexes on application startup.
/// Ensures optimal query execution and enforces unique constraints (e.g. Email, TrackingId).
/// </summary>
public class MongoDbInitializer
{
    private readonly IMongoDbContext _context;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(IMongoDbContext context, ILogger<MongoDbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Configuring MongoDB indexes for ParcelDeliveryDB...");

            // 1. Users Indexes
            var userEmailIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "idx_user_email_unique" }
            );

            var userRoleIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Role),
                new CreateIndexOptions { Name = "idx_user_role" }
            );

            var userAvailabilityIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.AvailabilityStatus),
                new CreateIndexOptions { Name = "idx_user_availability" }
            );

            await _context.Users.Indexes.CreateManyAsync(new[] { userEmailIndex, userRoleIndex, userAvailabilityIndex });

            // 2. Addresses Indexes
            var addressUserIndex = new CreateIndexModel<Address>(
                Builders<Address>.IndexKeys.Ascending(a => a.UserId),
                new CreateIndexOptions { Name = "idx_address_userId" }
            );

            await _context.Addresses.Indexes.CreateOneAsync(addressUserIndex);

            // 3. Parcels Indexes
            var parcelTrackingIndex = new CreateIndexModel<Parcel>(
                Builders<Parcel>.IndexKeys.Ascending(p => p.TrackingId),
                new CreateIndexOptions { Unique = true, Name = "idx_parcel_trackingId_unique" }
            );

            var parcelCustomerIndex = new CreateIndexModel<Parcel>(
                Builders<Parcel>.IndexKeys.Ascending(p => p.CustomerId),
                new CreateIndexOptions { Name = "idx_parcel_customerId" }
            );

            var parcelAgentIndex = new CreateIndexModel<Parcel>(
                Builders<Parcel>.IndexKeys.Ascending(p => p.AssignedAgentId),
                new CreateIndexOptions { Name = "idx_parcel_agentId" }
            );

            var parcelStatusIndex = new CreateIndexModel<Parcel>(
                Builders<Parcel>.IndexKeys.Ascending(p => p.Status),
                new CreateIndexOptions { Name = "idx_parcel_status" }
            );

            var parcelCreatedAtIndex = new CreateIndexModel<Parcel>(
                Builders<Parcel>.IndexKeys.Descending(p => p.CreatedAt),
                new CreateIndexOptions { Name = "idx_parcel_createdAt_desc" }
            );

            await _context.Parcels.Indexes.CreateManyAsync(new[] 
            { 
                parcelTrackingIndex, 
                parcelCustomerIndex, 
                parcelAgentIndex, 
                parcelStatusIndex, 
                parcelCreatedAtIndex 
            });

            // 4. Notifications Indexes
            var notifUserReadIndex = new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys.Ascending(n => n.UserId).Ascending(n => n.IsRead),
                new CreateIndexOptions { Name = "idx_notification_user_isRead" }
            );

            var notifCreatedAtIndex = new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys.Descending(n => n.CreatedAt),
                new CreateIndexOptions { Name = "idx_notification_createdAt_desc" }
            );

            await _context.Notifications.Indexes.CreateManyAsync(new[] { notifUserReadIndex, notifCreatedAtIndex });

            _logger.LogInformation("MongoDB indexes initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB index initialization encountered a non-fatal warning (or database is starting up). Application will continue.");
        }
    }
}
