using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalHub.Data;

namespace RentalHub.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? ciudad, DateTime? entrada, DateTime? salida)
    {
        // Si es Owner redirigir al dashboard
        if (User.IsInRole("Owner"))
            return RedirectToAction("Index", "Dashboard", new { area = "Owner" });

        var properties = await _context.Properties
            .Where(p => p.IsAvailable && !string.IsNullOrEmpty(p.Title))
            .Include(p => p.Images)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(ciudad))
            properties = properties
                .Where(p => p.City.ToLower().Contains(ciudad.ToLower()))
                .ToList();

        if (entrada.HasValue && salida.HasValue)
        {
            var reservedIds = await _context.Reservations
                .Where(r =>
                    r.Status != ReservationStatus.Cancelled &&
                    r.CheckIn < salida.Value &&
                    r.CheckOut > entrada.Value)
                .Select(r => r.PropertyId)
                .ToListAsync();

            properties = properties
                .Where(p => !reservedIds.Contains(p.Id))
                .ToList();
        }

        ViewBag.Ciudad  = ciudad;
        ViewBag.Entrada = entrada?.ToString("yyyy-MM-dd");
        ViewBag.Salida  = salida?.ToString("yyyy-MM-dd");

        return View(properties);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property == null) return NotFound();

        return View(property);
    }
}
