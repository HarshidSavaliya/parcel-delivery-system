using FluentAssertions;
using ParcelDeliverySystem.Services.Implementations;
using Xunit;

namespace ParcelDeliverySystem.Tests.Services;

public class BcryptPasswordHasherServiceTests
{
    private readonly BcryptPasswordHasherService _hasher;

    public BcryptPasswordHasherServiceTests()
    {
        _hasher = new BcryptPasswordHasherService();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedString()
    {
        // Arrange
        var rawPassword = "SecurePassword@123";

        // Act
        var hash = _hasher.HashPassword(rawPassword);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2");
        hash.Should().NotBe(rawPassword);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var rawPassword = "AdminPassword#456";
        var hash = _hasher.HashPassword(rawPassword);

        // Act
        var isValid = _hasher.VerifyPassword(rawPassword, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var rawPassword = "CorrectPassword123";
        var hash = _hasher.HashPassword(rawPassword);

        // Act
        var isValid = _hasher.VerifyPassword("WrongPassword123", hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPassword_NullOrEmptyInputs_ReturnsFalse(string? password)
    {
        // Act
        var isValid = _hasher.VerifyPassword(password!, "$2a$12$somehashhere");

        // Assert
        isValid.Should().BeFalse();
    }
}
