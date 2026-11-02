using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParcelDeliverySystem.Models.Enums;
using ParcelDeliverySystem.Services.Interfaces;
using ParcelDeliverySystem.ViewModels.Auth;
using ParcelDeliverySystem.ViewModels.Users;

namespace ParcelDeliverySystem.Controllers;

/// <summary>
/// Handles authentication workflows, role sign-in, account registration, profile management, and logout.
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        IUserService userService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToUserDashboard();
        }

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ValidateUserCredentialsAsync(model.Email, model.Password);

        if (!result.Succeeded || result.User == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid email or password.");
            return View(model);
        }

        // Create Claims Principal
        var principal = _authService.CreateClaimsPrincipal(result.User, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe 
                ? DateTimeOffset.UtcNow.AddDays(14) 
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties
        );

        _logger.LogInformation("User {Email} successfully authenticated with role {Role}", result.User.Email, result.User.Role);
        TempData["SuccessMessage"] = $"Welcome back, {result.User.FullName}!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToRoleDashboard(result.User.Role);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToUserDashboard();
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterCustomerAsync(model);

        if (!result.Succeeded || result.User == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed. Please try again.");
            return View(model);
        }

        // Automatically sign the user in upon successful registration
        var principal = _authService.CreateClaimsPrincipal(result.User, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        TempData["SuccessMessage"] = "Registration successful! Welcome to Parcel Delivery System.";
        return RedirectToAction("Dashboard", "Customer");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterAgent()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToUserDashboard();
        }

        return View(new AgentRegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterAgent(AgentRegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAgentAsync(model);

        if (!result.Succeeded || result.User == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Agent registration failed.");
            return View(model);
        }

        // Sign agent in
        var principal = _authService.CreateClaimsPrincipal(result.User, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        TempData["SuccessMessage"] = "Delivery Partner account registered! You can now access your Agent Portal.";
        return RedirectToAction("Dashboard", "DeliveryAgent");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name ?? "User";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {User} logged out.", userName);
        TempData["InfoMessage"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }

        var profile = await _userService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound();
        }

        return View(profile);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || userId != model.Id)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _userService.UpdateProfileAsync(userId, model);
        if (success)
        {
            TempData["SuccessMessage"] = "Profile details updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(string.Empty, "Unable to update profile. Please try again.");
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }

        var success = await _authService.ChangePasswordAsync(userId, model);
        if (success)
        {
            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
        return View(model);
    }

    private IActionResult RedirectToUserDashboard()
    {
        if (User.IsInRole("Admin"))
            return RedirectToAction("Dashboard", "Admin");
        if (User.IsInRole("DeliveryAgent"))
            return RedirectToAction("Dashboard", "DeliveryAgent");
        return RedirectToAction("Dashboard", "Customer");
    }

    private IActionResult RedirectToRoleDashboard(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
            UserRole.DeliveryAgent => RedirectToAction("Dashboard", "DeliveryAgent"),
            _ => RedirectToAction("Dashboard", "Customer")
        };
    }
}
