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
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string periodo = "mes")
    {
        var ownerId = _userManager.GetUserId(User);

        var ahora = DateTime.UtcNow;
        DateTime desde = periodo switch
        {
            "semana" => ahora.AddDays(-7),
            "mes"    => ahora.AddMonths(-1),
            "anio"   => ahora.AddYears(-1),
            "todo"   => DateTime.MinValue,
            _        => ahora.AddMonths(-1)
        };

        var propertyIds = await _context.Properties
            .Where(p => p.OwnerId == ownerId)
            .Select(p => p.Id)
            .ToListAsync();

        var todasReservations = await _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .Where(r => propertyIds.Contains(r.PropertyId))
            .ToListAsync();

        var reservasPeriodo = todasReservations
            .Where(r => r.CheckIn >= desde)
            .OrderByDescending(r => r.CheckIn)
            .ToList();

        // Tasa de ocupacion: dias ocupados / dias totales del periodo
        var diasPeriodo = (ahora - desde).TotalDays;
        if (diasPeriodo <= 0) diasPeriodo = 365;

        var diasOcupados = todasReservations
            .Where(r => r.Status == ReservationStatus.Confirmed &&
                        r.CheckIn >= desde)
            .Sum(r => (r.CheckOut - r.CheckIn).Days);

        var totalDiasDisponibles = propertyIds.Count * diasPeriodo;
        var tasaOcupacion = totalDiasDisponibles > 0
            ? Math.Round((diasOcupados / totalDiasDisponibles) * 100, 1)
            : 0;

        // Ingresos por mes para grafico
        var ingresosPorMes = todasReservations
            .Where(r => r.Status == ReservationStatus.Confirmed &&
                        r.CheckIn >= ahora.AddMonths(-6))
            .GroupBy(r => new { r.CheckIn.Year, r.CheckIn.Month })
            .Select(g => new
            {
                Mes      = $"{g.Key.Month:00}/{g.Key.Year}",
                Ingresos = g.Sum(r => (r.CheckOut - r.CheckIn).Days * r.Property.PricePerNight)
            })
            .OrderBy(x => x.Mes)
            .ToList();

        var vm = new DashboardViewModel
        {
            TotalProperties     = propertyIds.Count,
            TotalReservations   = reservasPeriodo.Count,
            PendingReservations = reservasPeriodo.Count(r => r.Status == ReservationStatus.Pending),
            TotalRevenue        = reservasPeriodo
                                    .Where(r => r.Status == ReservationStatus.Confirmed)
                                    .Sum(r => (r.CheckOut - r.CheckIn).Days * r.Property.PricePerNight),
            RecentReservations  = reservasPeriodo.Take(5).Select(r => new RecentReservationItem
            {
                GuestName     = r.User.FullName,
                PropertyTitle = r.Property.Title,
                CheckIn       = r.CheckIn,
                CheckOut      = r.CheckOut,
                Status        = r.Status
            }).ToList()
        };

        ViewBag.Periodo          = periodo;
        ViewBag.TasaOcupacion    = tasaOcupacion;
        ViewBag.IngresosPorMes   = ingresosPorMes;

        return View(vm);
    }
}
