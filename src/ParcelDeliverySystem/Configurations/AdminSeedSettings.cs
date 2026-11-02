namespace ParcelDeliverySystem.Configurations;

/// <summary>
/// Configuration for initial system seeding (e.g. default Admin and demo accounts).
/// Binds directly to the "AdminSeed" section of appsettings.json.
/// </summary>
public class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; set; } = true;
    public string Email { get; set; } = "admin@parceldelivery.com";
    public string Password { get; set; } = "Admin@123456";
    public string FullName { get; set; } = "System Administrator";
    public string PhoneNumber { get; set; } = "+1-800-555-0199";
    public bool SeedDemoAccounts { get; set; } = true;
}
