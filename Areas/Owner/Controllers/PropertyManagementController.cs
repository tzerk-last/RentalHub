using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalHub.Data;
using RentalHub.Models;
using RentalHub.ViewModels;

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
        var userId = _userManager.GetUserId(User)!;
        var kyc    = await _context.KycVerifications
            .FirstOrDefaultAsync(k => k.UserId == userId);
        if (kyc == null || kyc.Status != "Approved")
        {
            TempData["Error"] = "Debes verificar tu identidad antes de publicar inmuebles.";
            return RedirectToAction("Index", "Kyc", new { area = "Owner" });
        }
        return View(new PropertyViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(PropertyViewModel vm)
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
        ModelState.Remove("ExistingImages");
        if (!ModelState.IsValid) return View(vm);

        var property = new Property
        {
            Title         = vm.Title,
            Description   = vm.Description,
            City          = vm.City,
            Address       = vm.Address,
            PricePerNight = vm.PricePerNight,
            MaxGuests     = vm.MaxGuests,
            Bedrooms      = vm.Bedrooms,
            Bathrooms     = vm.Bathrooms,
            IsAvailable   = vm.IsAvailable,
            OwnerId       = userId
        };

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();
        await SaveImages(vm.Images, property.Id);

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

        var vm = new PropertyViewModel
        {
            Id             = property.Id,
            Title          = property.Title,
            Description    = property.Description,
            City           = property.City,
            Address        = property.Address,
            PricePerNight  = property.PricePerNight,
            MaxGuests      = property.MaxGuests,
            Bedrooms       = property.Bedrooms,
            Bathrooms      = property.Bathrooms,
            IsAvailable    = property.IsAvailable,
            ExistingImages = property.Images.ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PropertyViewModel vm)
    {
        ModelState.Remove("Images");
        ModelState.Remove("ExistingImages");
        if (!ModelState.IsValid)
        {
            vm.ExistingImages = await _context.PropertyImages
                .Where(i => i.PropertyId == id)
                .ToListAsync();
            return View(vm);
        }

        var existing = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null) return NotFound();

        existing.Title         = vm.Title;
        existing.Description   = vm.Description;
        existing.City          = vm.City;
        existing.Address       = vm.Address;
        existing.PricePerNight = vm.PricePerNight;
        existing.MaxGuests     = vm.MaxGuests;
        existing.Bedrooms      = vm.Bedrooms;
        existing.Bathrooms     = vm.Bathrooms;
        existing.IsAvailable   = vm.IsAvailable;

        await _context.SaveChangesAsync();

        if (vm.Images != null && vm.Images.Any())
            await SaveImages(vm.Images, id);

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
