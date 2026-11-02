using Microsoft.AspNetCore.Authentication.Cookies;
using ParcelDeliverySystem.Configurations;
using ParcelDeliverySystem.Data;
using ParcelDeliverySystem.Middleware;
using ParcelDeliverySystem.Repositories.Implementations;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using ParcelDeliverySystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. Strongly Typed Configuration Binding
// ==============================================================================
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.Configure<GeoapifySettings>(
    builder.Configuration.GetSection(GeoapifySettings.SectionName));
builder.Services.Configure<AdminSeedSettings>(
    builder.Configuration.GetSection(AdminSeedSettings.SectionName));
builder.Services.Configure<DeliveryPricingSettings>(
    builder.Configuration.GetSection(DeliveryPricingSettings.SectionName));

// ==============================================================================
// 2. MongoDB Driver & Infrastructure Registration
// ==============================================================================
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton<MongoDbInitializer>();
builder.Services.AddScoped<DbSeeder>();

// ==============================================================================
// 3. Repository Layer Registration
// ==============================================================================
builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IParcelRepository, ParcelRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAgentAssignmentRepository, AgentAssignmentRepository>();
builder.Services.AddScoped<IDeliveryRecordRepository, DeliveryRecordRepository>();

// ==============================================================================
// 4. Service Layer Registration
// ==============================================================================
builder.Services.AddSingleton<IPasswordHasherService, BcryptPasswordHasherService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITrackingIdGenerator, TrackingIdGenerator>();
builder.Services.AddScoped<IDeliveryChargeService, DeliveryChargeService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IParcelBookingService, ParcelBookingService>();
builder.Services.AddScoped<IDeliveryAgentService, DeliveryAgentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// Geoapify HTTP Client & Services
builder.Services.AddHttpClient<GeoapifyService>();
builder.Services.AddScoped<IGeocodingService>(sp => sp.GetRequiredService<GeoapifyService>());
builder.Services.AddScoped<IRoutingService>(sp => sp.GetRequiredService<GeoapifyService>());

// ==============================================================================
// 5. Authentication & Cookie Configuration
// ==============================================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ParcelDeliverySystem.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireDeliveryAgent", policy => policy.RequireRole("DeliveryAgent"));
    options.AddPolicy("RequireCustomer", policy => policy.RequireRole("Customer"));
});

// ==============================================================================
// 6. MVC Controller & Views Registration
// ==============================================================================
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==============================================================================
// 7. Database Initialization & Seeding on Startup
// ==============================================================================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking database indexes and initial seeder...");
        var indexInitializer = scope.ServiceProvider.GetRequiredService<MongoDbInitializer>();
        await indexInitializer.InitializeIndexesAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database connection or seeding note: Please ensure MongoDB is running.");
    }
}

// ==============================================================================
// 8. HTTP Request Pipeline Configuration
// ==============================================================================
// Global custom exception handling middleware
app.UseGlobalExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
