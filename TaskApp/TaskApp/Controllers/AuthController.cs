using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Models;
using TaskApp.Services;

namespace TaskApp.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        var result = await _authService.RegisterAsync(request);
        if (result is null)
        {
            ModelState.AddModelError(nameof(request.Email), "Email is already registered.");
            return View(request);
        }

        SetAuthCookie(result.Token);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        var result = await _authService.LoginAsync(request);
        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(request);
        }

        SetAuthCookie(result.Token);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token");
        return RedirectToAction(nameof(Login));
    }

    private void SetAuthCookie(string token) =>
        Response.Cookies.Append("auth_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });
}
