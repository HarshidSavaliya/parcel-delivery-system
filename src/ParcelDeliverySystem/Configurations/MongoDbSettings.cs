namespace ParcelDeliverySystem.Configurations;

/// <summary>
/// Strongly-typed settings for MongoDB Atlas connection and collection names.
/// Binds directly to the "MongoDbSettings" section of appsettings.json.
/// </summary>
public class MongoDbSettings
{
    public const string SectionName = "MongoDbSettings";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "ParcelDeliveryDB";

    public string UsersCollection { get; set; } = "Users";
    public string AddressesCollection { get; set; } = "Addresses";
    public string ParcelsCollection { get; set; } = "Parcels";
    public string NotificationsCollection { get; set; } = "Notifications";
    public string DeliveryRecordsCollection { get; set; } = "DeliveryRecords";
    public string AgentAssignmentsCollection { get; set; } = "AgentAssignments";
}
