using ParcelDeliverySystem.Models.Entities;

namespace ParcelDeliverySystem.ViewModels.Auth;

/// <summary>
/// Result object returned by authentication operations.
/// </summary>
public class AuthResultViewModel
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }

    public static AuthResultViewModel Success(User user) => new() { Succeeded = true, User = user };
    public static AuthResultViewModel Failure(string errorMessage) => new() { Succeeded = false, ErrorMessage = errorMessage };
}
