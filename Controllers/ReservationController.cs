using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalHub.Data;
using RentalHub.Models;
using RentalHub.Services.Interfaces;

namespace RentalHub.Controllers;

[Authorize]
public class ReservationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public ReservationController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> userManager,
                                 IEmailService emailService)
    {
        _context      = context;
        _userManager  = userManager;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(int propertyId, DateTime checkIn, DateTime checkOut)
    {
        var userId = _userManager.GetUserId(User)!;

        var kyc = await _context.KycVerifications
            .FirstOrDefaultAsync(k => k.UserId == userId);

        if (kyc == null || kyc.Status != "Approved")
        {
            TempData["Error"] = "Debes verificar tu identidad antes de realizar una reserva.";
            return RedirectToAction("Upload", "Kyc");
        }

        var checkInUtc  = DateTime.SpecifyKind(checkIn.Date.AddHours(14), DateTimeKind.Utc);
        var checkOutUtc = DateTime.SpecifyKind(checkOut.Date.AddHours(12), DateTimeKind.Utc);

        if (checkOutUtc <= checkInUtc)
        {
            TempData["Error"] = "La fecha de salida debe ser posterior a la entrada.";
            return RedirectToAction("Detail", "Home", new { id = propertyId });
        }

        var conflict = await _context.Reservations.AnyAsync(r =>
            r.PropertyId == propertyId &&
            r.Status != ReservationStatus.Cancelled &&
            r.CheckIn < checkOutUtc &&
            r.CheckOut > checkInUtc);

        if (conflict)
        {
            TempData["Error"] = "El inmueble no esta disponible para esas fechas.";
            return RedirectToAction("Detail", "Home", new { id = propertyId });
        }

        var property = await _context.Properties.FindAsync(propertyId);
        if (property == null) return NotFound();

        var reservation = new Reservation
        {
            PropertyId = propertyId,
            UserId     = userId,
            CheckIn    = checkInUtc,
            CheckOut   = checkOutUtc,
            Status     = ReservationStatus.Pending
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return RedirectToAction("Payment", new { id = reservation.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Payment(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        if (reservation.UserId != userId) return Forbid();

        var nights = (reservation.CheckOut - reservation.CheckIn).Days;
        ViewBag.Nights = nights;
        ViewBag.Total  = nights * reservation.Property.PricePerNight;

        return View(reservation);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null || reservation.UserId != user.Id) return Forbid();

        reservation.Status = ReservationStatus.Confirmed;

        var nights = (reservation.CheckOut - reservation.CheckIn).Days;
        var total  = nights * reservation.Property.PricePerNight;

        _context.Notifications.Add(new Notification
        {
            UserId    = user.Id,
            Title     = "Reserva confirmada",
            Message   = $"Tu reserva en {reservation.Property.Title} fue confirmada. Check-in: {reservation.CheckIn:dd/MM/yyyy} 2:00 PM.",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });

        _context.Notifications.Add(new Notification
        {
            UserId    = reservation.Property.OwnerId,
            Title     = "Nueva reserva",
            Message   = $"{user.FullName} reservo {reservation.Property.Title} del {reservation.CheckIn:dd/MM/yyyy} al {reservation.CheckOut:dd/MM/yyyy}.",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var body = $@"
            <h2>Reserva confirmada</h2>
            <p>Hola {user.FullName},</p>
            <table style='border-collapse:collapse;width:100%'>
                <tr><td style='padding:8px;border:1px solid #ddd'><b>Inmueble</b></td>
                    <td style='padding:8px;border:1px solid #ddd'>{reservation.Property.Title}</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd'><b>Check-in</b></td>
                    <td style='padding:8px;border:1px solid #ddd'>{reservation.CheckIn:dd/MM/yyyy} 2:00 PM</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd'><b>Check-out</b></td>
                    <td style='padding:8px;border:1px solid #ddd'>{reservation.CheckOut:dd/MM/yyyy} 12:00 PM</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd'><b>Noches</b></td>
                    <td style='padding:8px;border:1px solid #ddd'>{nights}</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd'><b>Total</b></td>
                    <td style='padding:8px;border:1px solid #ddd'>{total:C}</td></tr>
            </table>
            <p>Gracias por usar RentalHub.</p>";

        try
        {
            await _emailService.SendAsync(user.Email!, user.FullName,
                "Reserva confirmada - RentalHub", body);
        }
        catch { }

        TempData["Success"] = "Reserva confirmada. Te enviamos un correo con los detalles.";
        return RedirectToAction("MyReservations");
    }

    [HttpGet]
    public async Task<IActionResult> MyReservations()
    {
        var userId = _userManager.GetUserId(User)!;
        var reservations = await _context.Reservations
            .Where(r => r.UserId == userId)
            .Include(r => r.Property)
            .OrderByDescending(r => r.CheckIn)
            .ToListAsync();
        return View(reservations);
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var user   = await _userManager.GetUserAsync(User);

        var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reservation == null) return NotFound();

        var nights = (reservation.CheckOut - reservation.CheckIn).Days;
        ViewBag.Nights = nights;
        ViewBag.Total  = nights * reservation.Property.PricePerNight;
        ViewBag.User   = user;

        return View(reservation);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (reservation == null) return NotFound();
        reservation.Status = ReservationStatus.Cancelled;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Reserva cancelada.";
        return RedirectToAction("MyReservations");
    }
}
