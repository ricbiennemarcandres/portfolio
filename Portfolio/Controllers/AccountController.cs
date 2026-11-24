using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using portfolio.ViewModels;

namespace portfolio.Controllers;

/// <summary>
/// Signing in to the editor side. The password is read from configuration
/// ("Editor:Password"), which is kept in user-secrets — never in the repository.
/// </summary>
public class AccountController : Controller
{
    private readonly IConfiguration _config;

    public AccountController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home", new { area = "Editor" });
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")] // 5 attempts per minute per address (Program.cs)
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var expected = _config["Editor:Password"];
        if (string.IsNullOrEmpty(expected))
        {
            ModelState.AddModelError(string.Empty,
                "The editor password has not been set. Run: dotnet user-secrets set \"Editor:Password\" \"your-password\"");
            return View(model);
        }

        if (!PasswordMatches(model.Password, expected))
        {
            await Task.Delay(600); // slows down guessing
            ModelState.AddModelError(nameof(model.Password), "Wrong password.");
            return View(model);
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "Editor"), new Claim(ClaimTypes.Role, "Editor") },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                      new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "Editor" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    // Compares in constant time so the answer does not leak how many characters were right.
    private static bool PasswordMatches(string given, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));
}
