using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalHub.Models;
using RentalHub.ViewModels;

namespace RentalHub.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email    = model.Email,
            FullName = model.FullName,
            Role     = model.Role
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }

        var validRole = model.Role == Roles.Owner ? Roles.Owner : Roles.User;
        await _userManager.AddToRoleAsync(user, validRole);
        await _signInManager.SignInAsync(user, isPersistent: false);

        // Redirigir segun rol
        if (validRole == Roles.Owner)
            return RedirectToAction("Index", "Dashboard", new { area = "Owner" });

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password,
            isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Email o contrasena incorrectos");
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        if (await _userManager.IsInRoleAsync(user!, Roles.Owner))
            return RedirectToAction("Index", "Dashboard", new { area = "Owner" });

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
