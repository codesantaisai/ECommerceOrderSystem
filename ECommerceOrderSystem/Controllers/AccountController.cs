using ECommerceOrderSystem.Common;
using ECommerceOrderSystem.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

[AllowAnonymous]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectForRole();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLogin = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return await RedirectForUserAsync(user);
        }

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Your account is temporarily locked. Try again in 5 minutes.");
        else if (result.IsNotAllowed)
            ModelState.AddModelError(string.Empty, "This account is not allowed to sign in.");
        else
            ModelState.AddModelError(string.Empty, "Invalid email or password.");

        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectForRole();

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            LastLogin = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "CUSTOMER");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddErrors(roleResult);
            return View(model);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Products");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task<IActionResult> RedirectForUserAsync(ApplicationUser user)
    {
        if (await userManager.IsInRoleAsync(user, "ADMIN"))
            return RedirectToAction("All", "Orders");

        return RedirectToAction("Index", "Products");
    }

    private IActionResult RedirectForRole() =>
        User.IsInRole("ADMIN")
            ? RedirectToAction("All", "Orders")
            : RedirectToAction("Index", "Products");

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}
