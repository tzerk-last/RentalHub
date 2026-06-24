using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalHub.Data;
using RentalHub.Models;

namespace RentalHub.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Roles = "Owner")]
public class PropertyManagementController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public PropertyManagementController(ApplicationDbContext context,
                                        UserManager<ApplicationUser> userManager,
                                        IWebHostEnvironment env)
    {
        _context     = context;
        _userManager = userManager;
        _env         = env;
    }

    public async Task<IActionResult> Index()
    {
        var ownerId    = _userManager.GetUserId(User);
        var properties = await _context.Properties
            .Where(p => p.OwnerId == ownerId)
            .Include(p => p.Images)
            .ToListAsync();
        return View(properties);
    }

    public async Task<IActionResult> Create()
    {
        // Verificar KYC
        var userId = _userManager.GetUserId(User)!;
        var kyc    = await _context.KycVerifications
            .FirstOrDefaultAsync(k => k.UserId == userId);

        if (kyc == null || kyc.Status != "Approved")
        {
            TempData["Error"] = "Debes verificar tu identidad antes de publicar inmuebles.";
            return RedirectToAction("Index", "Kyc", new { area = "Owner" });
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Property property, List<IFormFile>? images)
    {
        var userId = _userManager.GetUserId(User)!;
        var kyc    = await _context.KycVerifications
            .FirstOrDefaultAsync(k => k.UserId == userId);

        if (kyc == null || kyc.Status != "Approved")
        {
            TempData["Error"] = "Debes verificar tu identidad antes de publicar inmuebles.";
            return RedirectToAction("Index", "Kyc", new { area = "Owner" });
        }

        ModelState.Remove("Images");
        ModelState.Remove("Reservations");
        ModelState.Remove("OwnerId");

        if (!ModelState.IsValid) return View(property);

        property.OwnerId = userId;
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        await SaveImages(images, property.Id);

        TempData["Success"] = "Inmueble creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ownerId  = _userManager.GetUserId(User);
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
        if (property == null) return NotFound();
        return View(property);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Property property, List<IFormFile>? images)
    {
        if (id != property.Id) return BadRequest();

        ModelState.Remove("Images");
        ModelState.Remove("Reservations");
        ModelState.Remove("OwnerId");

        if (!ModelState.IsValid) return View(property);

        property.OwnerId = _userManager.GetUserId(User)!;
        _context.Properties.Update(property);
        await _context.SaveChangesAsync();

        await SaveImages(images, property.Id);

        TempData["Success"] = "Inmueble actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var ownerId  = _userManager.GetUserId(User);
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
        if (property == null) return NotFound();
        return View(property);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ownerId  = _userManager.GetUserId(User);
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
        if (property != null)
        {
            foreach (var img in property.Images)
            {
                var path = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
        }
        TempData["Success"] = "Inmueble eliminado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int imageId, int propertyId)
    {
        var image = await _context.PropertyImages.FindAsync(imageId);
        if (image != null)
        {
            var path = Path.Combine(_env.WebRootPath, image.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            _context.PropertyImages.Remove(image);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit), new { id = propertyId });
    }

    private async Task SaveImages(List<IFormFile>? images, int propertyId)
    {
        if (images == null || !images.Any()) return;

        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "properties");
        Directory.CreateDirectory(uploadPath);

        foreach (var file in images)
        {
            if (file.Length == 0) continue;

            var ext     = Path.GetExtension(file.FileName).ToLower();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext)) continue;

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _context.PropertyImages.Add(new PropertyImage
            {
                PropertyId = propertyId,
                ImageUrl   = $"/uploads/properties/{fileName}"
            });
        }

        await _context.SaveChangesAsync();
    }
}
