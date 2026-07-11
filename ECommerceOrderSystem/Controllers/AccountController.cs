using System.Security.Claims;
using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Common;
using ECommerceOrderSystem.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IJwtService jwtService, ILogger<AccountController> logger) : Controller
{
    private const string TokenCookie = "access_token";

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        User.Identity?.IsAuthenticated == true ? RedirectForRole() : View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if(!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if(user is null)
        {
            logger.LogWarning("Failed login attempt for unknown email {Email}.", model.Email.Trim());
            ModelState.AddModelError(string.Empty, "Invalid email or password."); return View(model);
        }

        var result = await signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if(!result.Succeeded)
        {
            logger.LogWarning("Failed login attempt for user {UserId}. LockedOut: {IsLockedOut}.", user.Id, result.IsLockedOut);
            ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Account temporarily locked." : "Invalid email or password.");
            return View(model);
        }

        user.LastLogin = DateTime.UtcNow;
        await users.UpdateAsync(user);
        await SetTokenCookie(user, model.RememberMe);
        logger.LogInformation("User {UserId} logged in successfully.", user.Id);
        if(!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return await users.IsInRoleAsync(user, "ADMIN") ? RedirectToAction("All", "Orders") : RedirectToAction("Index", "Products");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectForRole() : View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if(!ModelState.IsValid) return View(model);
        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            LastLogin = DateTime.UtcNow
        };
        var result = await users.CreateAsync(user, model.Password);
        if(!result.Succeeded)
        {
            logger.LogWarning("Registration failed for email {Email}.", model.Email.Trim());
            foreach(var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        var roleResult = await users.AddToRoleAsync(user, "CUSTOMER");
        if(!roleResult.Succeeded)
        {
            logger.LogError("Failed to assign CUSTOMER role to new user {UserId}.", user.Id);
            await users.DeleteAsync(user);
            foreach(var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        await SetTokenCookie(user, false);
        logger.LogInformation("New customer account {UserId} registered successfully.", user.Id);
        return RedirectToAction("Index", "Products");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        logger.LogInformation("User {UserId} logged out.", User.FindFirstValue(ClaimTypes.NameIdentifier));
        Response.Cookies.Delete(TokenCookie);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SetTokenCookie(ApplicationUser user, bool rememberMe)
    {
        var (token, expiresAt) = await jwtService.GenerateToken(user);
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true
        };
        if(rememberMe) options.Expires = expiresAt;
        Response.Cookies.Append(TokenCookie, token, options);
    }

    private IActionResult RedirectForRole() => User.IsInRole("ADMIN") ? RedirectToAction("All", "Orders") : RedirectToAction("Index", "Products");
}