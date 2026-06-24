using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalHub.Data;
using RentalHub.Models;

namespace RentalHub.Controllers;

public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WishlistController(ApplicationDbContext context,
                              UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Toggle(int propertyId)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action("Toggle", "Wishlist", new { propertyId }) });

        var userId = _userManager.GetUserId(User)!;

        var existing = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.PropertyId == propertyId);

        if (existing != null)
            _context.Wishlists.Remove(existing);
        else
            _context.Wishlists.Add(new Wishlist { UserId = userId, PropertyId = propertyId });

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var items = await _context.Wishlists
            .Where(w => w.UserId == userId)
            .Include(w => w.Property)
                .ThenInclude(p => p.Images)
            .ToListAsync();

        return View(items);
    }
}
