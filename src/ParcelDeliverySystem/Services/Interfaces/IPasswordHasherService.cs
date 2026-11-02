namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Cryptographic password hashing and verification contract.
/// Ensures zero plain-text password persistence and uses secure salted work-factor algorithms.
/// </summary>
public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
