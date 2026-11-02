using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Implementations;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Auth;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<INotificationRepository> _notifRepoMock;
    private readonly Mock<IPasswordHasherService> _hasherMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _notifRepoMock = new Mock<INotificationRepository>();
        _hasherMock = new Mock<IPasswordHasherService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userRepoMock.Object,
            _notifRepoMock.Object,
            _hasherMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task RegisterCustomerAsync_WhenEmailAlreadyTaken_ReturnsFailure()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "+1234567890",
            Password = "Password123"
        };

        _userRepoMock.Setup(r => r.IsEmailTakenAsync(model.Email.ToLowerInvariant(), null))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterCustomerAsync(model);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterCustomerAsync_WhenValid_HashesPasswordAndSavesUser()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            FullName = "Jane Smith",
            Email = "jane@example.com",
            PhoneNumber = "+1987654321",
            Password = "SecurePass123"
        };

        _userRepoMock.Setup(r => r.IsEmailTakenAsync(model.Email.ToLowerInvariant(), null))
            .ReturnsAsync(false);

        _hasherMock.Setup(h => h.HashPassword(model.Password))
            .Returns("hashed_secure_pass");

        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = "507f1f77bcf86cd799439011"; return u; });

        // Act
        var result = await _authService.RegisterCustomerAsync(model);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Role.Should().Be(UserRole.Customer);
        result.User.PasswordHash.Should().Be("hashed_secure_pass");
        _notifRepoMock.Verify(n => n.CreateAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAgentAsync_WhenValid_SetsRoleToDeliveryAgentAndOfflineStatus()
    {
        // Arrange
        var model = new AgentRegisterViewModel
        {
            FullName = "Agent Cooper",
            Email = "cooper@fleet.com",
            PhoneNumber = "+15551234",
            VehicleType = "Van / Courier Express",
            VehiclePlateNumber = "ABC-1234",
            DrivingLicenseNumber = "DL-998877",
            Password = "AgentPass123"
        };

        _userRepoMock.Setup(r => r.IsEmailTakenAsync(model.Email.ToLowerInvariant(), null))
            .ReturnsAsync(false);

        _hasherMock.Setup(h => h.HashPassword(model.Password))
            .Returns("hashed_agent_pass");

        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = "507f1f77bcf86cd799439012"; return u; });

        // Act
        var result = await _authService.RegisterAgentAsync(model);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Role.Should().Be(UserRole.DeliveryAgent);
        result.User.AvailabilityStatus.Should().Be(AgentAvailabilityStatus.Offline);
        result.User.VehicleType.Should().Be("Van / Courier Express");
    }

    [Fact]
    public async Task ValidateUserCredentialsAsync_WhenCredentialsValid_ReturnsSuccess()
    {
        // Arrange
        var email = "admin@parceldelivery.com";
        var password = "AdminPassword@123";
        var user = new User
        {
            Id = "507f1f77bcf86cd799439013",
            FullName = "Admin User",
            Email = email,
            PasswordHash = "hashed_admin_pass",
            Role = UserRole.Admin,
            IsActive = true
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyPassword(password, user.PasswordHash)).Returns(true);

        // Act
        var result = await _authService.ValidateUserCredentialsAsync(email, password);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.User.Should().Be(user);
        _userRepoMock.Verify(r => r.UpdateLastLoginAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task ValidateUserCredentialsAsync_WhenUserInactive_ReturnsFailure()
    {
        // Arrange
        var email = "banned@example.com";
        var user = new User
        {
            Id = "507f1f77bcf86cd799439014",
            Email = email,
            IsActive = false
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);

        // Act
        var result = await _authService.ValidateUserCredentialsAsync(email, "somepass");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("deactivated");
    }

    [Fact]
    public void CreateClaimsPrincipal_IncludesRoleAndIdentifierClaims()
    {
        // Arrange
        var user = new User
        {
            Id = "507f1f77bcf86cd799439015",
            FullName = "David Agent",
            Email = "david@courier.com",
            Role = UserRole.DeliveryAgent,
            AvailabilityStatus = AgentAvailabilityStatus.Available
        };

        // Act
        var principal = _authService.CreateClaimsPrincipal(user, "TestScheme");

        // Assert
        principal.Should().NotBeNull();
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id);
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(user.FullName);
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
        principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be("DeliveryAgent");
        principal.FindFirst("AvailabilityStatus")?.Value.Should().Be("Available");
    }
}
