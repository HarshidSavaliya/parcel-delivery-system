using System.Security.Claims;
using ParcelDeliverySystem.Models.Entities;
using ParcelDeliverySystem.ViewModels.Auth;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Services.Interfaces;

/// <summary>
/// Business logic contract for user authentication, registration, password hashing, and claim generation.
/// </summary>
public interface IAuthService
{
    Task<AuthResultViewModel> RegisterCustomerAsync(RegisterViewModel model);
    Task<AuthResultViewModel> RegisterAgentAsync(AgentRegisterViewModel model);
    Task<AuthResultViewModel> ValidateUserCredentialsAsync(string email, string password);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordViewModel model);
    ClaimsPrincipal CreateClaimsPrincipal(User user, string authenticationScheme);
}
