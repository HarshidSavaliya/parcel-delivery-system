using System.Security.Claims;
using Microsoft.Extensions.Logging;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Auth;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Service layer implementation for authentication workflows.
/// Implements validation, cryptographic verification, and claims generation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IPasswordHasherService passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResultViewModel> RegisterCustomerAsync(RegisterViewModel model)
    {
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        if (await _userRepository.IsEmailTakenAsync(normalizedEmail))
        {
            _logger.LogWarning("Registration failed: Email {Email} is already in use.", normalizedEmail);
            return AuthResultViewModel.Failure("An account with this email address already exists.");
        }

        var customer = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = model.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.HashPassword(model.Password),
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(customer);

        // Send welcome notification
        var notification = new Notification
        {
            UserId = createdUser.Id,
            Title = "Welcome to Parcel Delivery System!",
            Message = "Your customer account was registered successfully. You can now book deliveries, manage addresses, and track parcels.",
            Type = NotificationType.General,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepository.CreateAsync(notification);

        _logger.LogInformation("New customer registered successfully: {Email} (Id: {Id})", createdUser.Email, createdUser.Id);
        return AuthResultViewModel.Success(createdUser);
    }

    public async Task<AuthResultViewModel> RegisterAgentAsync(AgentRegisterViewModel model)
    {
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        if (await _userRepository.IsEmailTakenAsync(normalizedEmail))
        {
            _logger.LogWarning("Agent registration failed: Email {Email} is already in use.", normalizedEmail);
            return AuthResultViewModel.Failure("An account with this email address already exists.");
        }

        var agent = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = model.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.HashPassword(model.Password),
            Role = UserRole.DeliveryAgent,
            IsActive = true,
            AvailabilityStatus = AgentAvailabilityStatus.Offline,
            VehicleType = model.VehicleType.Trim(),
            VehiclePlateNumber = model.VehiclePlateNumber.Trim(),
            DrivingLicenseNumber = model.DrivingLicenseNumber.Trim(),
            Rating = 5.0,
            CompletedDeliveriesCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        var createdAgent = await _userRepository.CreateAsync(agent);

        // Send welcome notification
        var notification = new Notification
        {
            UserId = createdAgent.Id,
            Title = "Partner Onboarding Received",
            Message = "Welcome to the Courier Fleet! Set your status to 'Available' in your dashboard when ready to accept deliveries.",
            Type = NotificationType.General,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepository.CreateAsync(notification);

        _logger.LogInformation("New delivery agent onboarded: {Email} (Id: {Id})", createdAgent.Email, createdAgent.Id);
        return AuthResultViewModel.Success(createdAgent);
    }

    public async Task<AuthResultViewModel> ValidateUserCredentialsAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResultViewModel.Failure("Invalid email or password.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found.", normalizedEmail);
            return AuthResultViewModel.Failure("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User account {Email} is disabled.", normalizedEmail);
            return AuthResultViewModel.Failure("Your account has been deactivated. Please contact support.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Incorrect password attempt for {Email}.", normalizedEmail);
            return AuthResultViewModel.Failure("Invalid email or password.");
        }

        // Update last login timestamp asynchronously
        await _userRepository.UpdateLastLoginAsync(user.Id);

        _logger.LogInformation("User logged in successfully: {Email} (Role: {Role})", user.Email, user.Role);
        return AuthResultViewModel.Success(user);
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordViewModel model)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        var isCurrentValid = _passwordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash);
        if (!isCurrentValid) return false;

        var newHash = _passwordHasher.HashPassword(model.NewPassword);
        return await _userRepository.UpdatePasswordHashAsync(userId, newHash);
    }

    public ClaimsPrincipal CreateClaimsPrincipal(User user, string authenticationScheme)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("PhoneNumber", user.PhoneNumber ?? string.Empty),
            new("RoleName", user.Role.ToString())
        };

        if (user.Role == UserRole.DeliveryAgent)
        {
            claims.Add(new Claim("AvailabilityStatus", user.AvailabilityStatus.ToString()));
            if (!string.IsNullOrEmpty(user.VehicleType))
                claims.Add(new Claim("VehicleType", user.VehicleType));
        }

        var identity = new ClaimsIdentity(claims, authenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
