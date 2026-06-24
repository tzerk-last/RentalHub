using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalHub.Data;
using RentalHub.Models;

namespace RentalHub.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Roles = "Owner")]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportController(ApplicationDbContext context,
                            UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ownerId = _userManager.GetUserId(User);

        var properties = await _context.Properties
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync();

        ViewBag.Properties = properties;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Download(int? propertyId)
    {
        var ownerId = _userManager.GetUserId(User);

        var query = _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .Where(r => r.Property.OwnerId == ownerId);

        if (propertyId.HasValue)
            query = query.Where(r => r.PropertyId == propertyId.Value);

        var reservations = await query
            .OrderByDescending(r => r.CheckIn)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Reporte");

        // Encabezados
        ws.Cell(1, 1).Value = "Inmueble";
        ws.Cell(1, 2).Value = "Ciudad";
        ws.Cell(1, 3).Value = "Huesped";
        ws.Cell(1, 4).Value = "Email";
        ws.Cell(1, 5).Value = "Check-in";
        ws.Cell(1, 6).Value = "Check-out";
        ws.Cell(1, 7).Value = "Noches";
        ws.Cell(1, 8).Value = "Precio/noche";
        ws.Cell(1, 9).Value = "Total";
        ws.Cell(1, 10).Value = "Estado";

        // Estilo encabezados
        var header = ws.Range(1, 1, 1, 10);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Datos
        int row = 2;
        foreach (var r in reservations)
        {
            var nights = (r.CheckOut - r.CheckIn).Days;
            var total  = nights * r.Property.PricePerNight;

            ws.Cell(row, 1).Value  = r.Property.Title;
            ws.Cell(row, 2).Value  = r.Property.City;
            ws.Cell(row, 3).Value  = r.User.FullName;
            ws.Cell(row, 4).Value  = r.User.Email;
            ws.Cell(row, 5).Value  = r.CheckIn.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value  = r.CheckOut.ToString("dd/MM/yyyy");
            ws.Cell(row, 7).Value  = nights;
            ws.Cell(row, 8).Value  = r.Property.PricePerNight;
            ws.Cell(row, 9).Value  = total;
            ws.Cell(row, 10).Value = r.Status;

            // Formato moneda
            ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";

            // Color fila segun estado
            var color = r.Status == ReservationStatus.Confirmed ? XLColor.FromHtml("#d4edda") :
                        r.Status == ReservationStatus.Pending   ? XLColor.FromHtml("#fff3cd") :
                                                                   XLColor.FromHtml("#f8d7da");
            ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = color;

            row++;
        }

        // Autoajustar columnas
        ws.Columns().AdjustToContents();

        // Totales al final
        ws.Cell(row, 8).Value = "TOTAL";
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 9).FormulaA1 = $"=SUM(I2:I{row - 1})";
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var fileName = propertyId.HasValue
            ? $"reporte_inmueble_{propertyId}.xlsx"
            : $"reporte_completo_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
