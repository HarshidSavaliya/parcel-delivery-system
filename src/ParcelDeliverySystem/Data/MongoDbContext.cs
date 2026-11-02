using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ParcelDeliverySystem.Configurations;
using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.Data;

/// <summary>
/// Concrete implementation of IMongoDbContext managing MongoDB connection pooling and collections.
/// Uses the official MongoDB .NET Driver.
/// </summary>
public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;
    private readonly MongoDbSettings _settings;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;

        var clientSettings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);
        // Optimize for production connection resiliency
        clientSettings.ServerApi = new ServerApi(ServerApiVersion.V1);
        clientSettings.MaxConnectionPoolSize = 100;
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
        clientSettings.SocketTimeout = TimeSpan.FromSeconds(30);

        _client = new MongoClient(clientSettings);
        _database = _client.GetDatabase(_settings.DatabaseName);

        Users = _database.GetCollection<User>(_settings.UsersCollection);
        Addresses = _database.GetCollection<Address>(_settings.AddressesCollection);
        Parcels = _database.GetCollection<Parcel>(_settings.ParcelsCollection);
        Notifications = _database.GetCollection<Notification>(_settings.NotificationsCollection);
        DeliveryRecords = _database.GetCollection<DeliveryRecord>(_settings.DeliveryRecordsCollection);
        AgentAssignments = _database.GetCollection<AgentAssignment>(_settings.AgentAssignmentsCollection);
    }

    public IMongoDatabase Database => _database;
    public IMongoClient Client => _client;

    public IMongoCollection<User> Users { get; }
    public IMongoCollection<Address> Addresses { get; }
    public IMongoCollection<Parcel> Parcels { get; }
    public IMongoCollection<Notification> Notifications { get; }
    public IMongoCollection<DeliveryRecord> DeliveryRecords { get; }
    public IMongoCollection<AgentAssignment> AgentAssignments { get; }

    public IMongoCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);
}
