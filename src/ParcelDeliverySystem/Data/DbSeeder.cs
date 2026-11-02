using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ParcelDeliverySystem.Configurations;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Services.Interfaces;

namespace ParcelDeliverySystem.Data;

/// <summary>
/// Seeds essential system entities (initial Administrator, demo Customer, and demo Agent)
/// upon first startup if the database is uninitialized.
/// </summary>
public class DbSeeder
{
    private readonly IMongoDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly AdminSeedSettings _adminSettings;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        IMongoDbContext context,
        IPasswordHasherService passwordHasher,
        IOptions<AdminSeedSettings> adminSettings,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _adminSettings = adminSettings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (!_adminSettings.Enabled)
        {
            _logger.LogInformation("Database seeding is disabled in configuration.");
            return;
        }

        try
        {
            // 1. Seed Administrator
            var adminEmail = _adminSettings.Email.Trim().ToLowerInvariant();
            var existingAdmin = await _context.Users
                .Find(u => u.Email == adminEmail || u.Role == UserRole.Admin)
                .FirstOrDefaultAsync();

            if (existingAdmin == null)
            {
                _logger.LogInformation("Seeding initial Super Administrator account: {Email}", adminEmail);

                var admin = new User
                {
                    FullName = _adminSettings.FullName,
                    Email = adminEmail,
                    PhoneNumber = _adminSettings.PhoneNumber,
                    PasswordHash = _passwordHasher.HashPassword(_adminSettings.Password),
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.InsertOneAsync(admin);

                // Add welcome notification
                var welcomeNotif = new Notification
                {
                    UserId = admin.Id,
                    Title = "System Initialized",
                    Message = "Parcel Delivery System has been deployed and initialized with full administrative privileges.",
                    Type = NotificationType.Alert,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Notifications.InsertOneAsync(welcomeNotif);

                _logger.LogInformation("Administrator account created successfully.");
            }

            // 2. Seed Demo Accounts if enabled
            if (_adminSettings.SeedDemoAccounts)
            {
                await SeedDemoCustomerAsync();
                await SeedDemoAgentAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database seeding. Check MongoDB connection.");
        }
    }

    private async Task SeedDemoCustomerAsync()
    {
        var customerEmail = "customer@parceldelivery.com";
        var existingCustomer = await _context.Users
            .Find(u => u.Email == customerEmail)
            .FirstOrDefaultAsync();

        if (existingCustomer == null)
        {
            _logger.LogInformation("Seeding demo Customer account: {Email}", customerEmail);
            var customer = new User
            {
                FullName = "Alice Walker (Customer)",
                Email = customerEmail,
                PhoneNumber = "+1-555-0144",
                PasswordHash = _passwordHasher.HashPassword("Customer@123"),
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(customer);

            // Default Address for customer
            var address = new Address
            {
                UserId = customer.Id,
                Label = "Home",
                RecipientName = "Alice Walker",
                PhoneNumber = "+1-555-0144",
                StreetAddress = "742 Evergreen Terrace",
                City = "Springfield",
                State = "OR",
                PostalCode = "97477",
                Country = "United States",
                FormattedAddress = "742 Evergreen Terrace, Springfield, OR 97477",
                Latitude = 44.0462,
                Longitude = -123.0220,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Addresses.InsertOneAsync(address);

            var notif = new Notification
            {
                UserId = customer.Id,
                Title = "Welcome to Parcel Delivery System!",
                Message = "Your customer account is active. You can now book parcels, manage saved addresses, and track shipments in real-time.",
                Type = NotificationType.General,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notifications.InsertOneAsync(notif);
        }
    }

    private async Task SeedDemoAgentAsync()
    {
        var agentEmail = "agent@parceldelivery.com";
        var existingAgent = await _context.Users
            .Find(u => u.Email == agentEmail)
            .FirstOrDefaultAsync();

        if (existingAgent == null)
        {
            _logger.LogInformation("Seeding demo Delivery Agent account: {Email}", agentEmail);
            var agent = new User
            {
                FullName = "Marcus Vance (Delivery Agent)",
                Email = agentEmail,
                PhoneNumber = "+1-555-0188",
                PasswordHash = _passwordHasher.HashPassword("Agent@123"),
                Role = UserRole.DeliveryAgent,
                IsActive = true,
                AvailabilityStatus = AgentAvailabilityStatus.Available,
                VehicleType = "Van / Courier Express",
                VehiclePlateNumber = "DL-7788-XP",
                DrivingLicenseNumber = "DL-US-9938210",
                Rating = 4.9,
                CompletedDeliveriesCount = 124,
                CurrentLatitude = 44.0521,
                CurrentLongitude = -123.0189,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(agent);

            var notif = new Notification
            {
                UserId = agent.Id,
                Title = "Onboarding Complete",
                Message = "You are currently marked as 'Available' to accept delivery requests in your operational zone.",
                Type = NotificationType.General,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notifications.InsertOneAsync(notif);
        }
    }
}
