using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalHub.Data;
using RentalHub.Models;
using RentalHub.Services.Interfaces;

namespace RentalHub.Controllers;

[Authorize(Roles = "User")]
public class KycController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IKycService _kycService;
    private readonly IEmailService _emailService;

    public KycController(ApplicationDbContext context,
                         UserManager<ApplicationUser> userManager,
                         IWebHostEnvironment env,
                         IKycService kycService,
                         IEmailService emailService)
    {
        _context      = context;
        _userManager  = userManager;
        _env          = env;
        _kycService   = kycService;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var kyc = await _context.KycVerifications
            .FirstOrDefaultAsync(k => k.UserId == userId);
        return View(kyc);
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        var userId   = _userManager.GetUserId(User)!;
        var existing = await _context.KycVerifications
            .FirstOrDefaultAsync(k => k.UserId == userId);
        if (existing != null && existing.Status == "Approved")
        {
            TempData["Success"] = "Tu identidad ya fue verificada.";
            return RedirectToAction("Index");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile document, IFormFile selfie)
    {
        var userId = _userManager.GetUserId(User)!;

        if (document == null || selfie == null)
        {
            ModelState.AddModelError("", "Debes subir el documento y la selfie.");
            return View();
        }

        var uploadPath = Path.Combine(_env.WebRootPath, "kyc", userId);
        Directory.CreateDirectory(uploadPath);

        var docExt  = Path.GetExtension(document.FileName).ToLower();
        var docName = $"doc_{Guid.NewGuid()}{docExt}";
        var docPath = Path.Combine(uploadPath, docName);
        using (var s = new FileStream(docPath, FileMode.Create))
            await document.CopyToAsync(s);

        var selExt  = Path.GetExtension(selfie.FileName).ToLower();
        var selName = $"sel_{Guid.NewGuid()}{selExt}";
        var selPath = Path.Combine(uploadPath, selName);
        using (var s = new FileStream(selPath, FileMode.Create))
            await selfie.CopyToAsync(s);

        var (approved, reason) = await _kycService.VerifyAsync(docPath, selPath);

        var existing = await _context.KycVerifications
            .FirstOrDefaultAsync(k => k.UserId == userId);
        if (existing != null)
            _context.KycVerifications.Remove(existing);

        var kyc = new KycVerification
        {
            UserId       = userId,
            DocumentPath = $"/kyc/{userId}/{docName}",
            SelfiePath   = $"/kyc/{userId}/{selName}",
            Status       = approved ? "Approved" : "Rejected",
            CreatedAt    = DateTime.UtcNow,
            ReviewedAt   = DateTime.UtcNow
        };

        _context.KycVerifications.Add(kyc);

        // Notificacion in-app
        _context.Notifications.Add(new Notification
        {
            UserId    = userId,
            Title     = approved ? "Identidad verificada" : "Verificacion rechazada",
            Message   = approved
                ? "Tu identidad fue verificada exitosamente. Ya puedes realizar reservas."
                : $"Tu verificacion fue rechazada: {reason}. Por favor intenta de nuevo.",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        DeleteFileSecure(docPath);
        DeleteFileSecure(selPath);

        var user = await _userManager.GetUserAsync(User);

        try
        {
            await _emailService.SendAsync(
                user!.Email!, user.FullName,
                approved ? "Identidad verificada - RentalHub" : "Verificacion rechazada - RentalHub",
                approved
                    ? $"<h2>Verificacion aprobada</h2><p>Hola {user.FullName}, tu identidad fue verificada. Ya puedes realizar reservas.</p>"
                    : $"<h2>Verificacion rechazada</h2><p>Hola {user.FullName}, tus documentos no pudieron verificarse: {reason}</p>"
            );
        }
        catch { }

        if (approved)
            TempData["Success"] = "Identidad verificada. Ya puedes realizar reservas.";
        else
            TempData["Error"] = $"Verificacion rechazada: {reason}. Intenta de nuevo.";

        return RedirectToAction("Index");
    }

    private void DeleteFileSecure(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path)) return;
            var size = new FileInfo(path).Length;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write))
            {
                var zeros = new byte[size];
                fs.Write(zeros, 0, zeros.Length);
            }
            System.IO.File.Delete(path);
        }
        catch { }
    }
}
