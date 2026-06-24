using Microsoft.EntityFrameworkCore;
using RentalHub.Data;
using RentalHub.Models;
using RentalHub.Services.Interfaces;

namespace RentalHub.Services.Implementations;

public class ReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(IServiceScopeFactory scopeFactory,
                           ILogger<ReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckReminders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReminderService");
            }

            // Revisar cada hora
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckReminders()
    {
        using var scope   = _scopeFactory.CreateScope();
        var context       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService  = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var ahora  = DateTime.UtcNow;
        var manana = ahora.AddHours(24);

        // Reservas con check-in en las proximas 24 horas
        var proximasLlegadas = await context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .Where(r =>
                r.Status == ReservationStatus.Confirmed &&
                r.CheckIn >= ahora &&
                r.CheckIn <= manana)
            .ToListAsync();

        foreach (var r in proximasLlegadas)
        {
            // Verificar que no se haya enviado ya
            var yaNotificado = await context.Notifications.AnyAsync(n =>
                n.UserId == r.UserId &&
                n.Title == "Recordatorio de llegada" &&
                n.Message.Contains(r.Id.ToString()));

            if (yaNotificado) continue;

            // Notificacion in-app
            context.Notifications.Add(new Notification
            {
                UserId    = r.UserId,
                Title     = "Recordatorio de llegada",
                Message   = $"[{r.Id}] Tu check-in en {r.Property.Title} es manana {r.CheckIn:dd/MM/yyyy} a las 2:00 PM. Direccion: {r.Property.Address}, {r.Property.City}.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            // Correo
            try
            {
                await emailService.SendAsync(
                    r.User.Email!,
                    r.User.FullName,
                    "Recordatorio de llegada - RentalHub",
                    $@"<h2>Tu llegada es manana</h2>
                    <p>Hola {r.User.FullName},</p>
                    <p>Te recordamos que manana tienes check-in en <b>{r.Property.Title}</b>.</p>
                    <table style='border-collapse:collapse;width:100%'>
                        <tr><td style='padding:8px;border:1px solid #ddd'><b>Direccion</b></td>
                            <td style='padding:8px;border:1px solid #ddd'>{r.Property.Address}, {r.Property.City}</td></tr>
                        <tr><td style='padding:8px;border:1px solid #ddd'><b>Check-in</b></td>
                            <td style='padding:8px;border:1px solid #ddd'>{r.CheckIn:dd/MM/yyyy} a las 2:00 PM</td></tr>
                        <tr><td style='padding:8px;border:1px solid #ddd'><b>Check-out</b></td>
                            <td style='padding:8px;border:1px solid #ddd'>{r.CheckOut:dd/MM/yyyy} a las 12:00 PM</td></tr>
                    </table>
                    <p>Buen viaje.</p>"
                );
            }
            catch { }
        }

        // Reservas con check-out en las proximas 24 horas
        var proximasSalidas = await context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .Where(r =>
                r.Status == ReservationStatus.Confirmed &&
                r.CheckOut >= ahora &&
                r.CheckOut <= manana)
            .ToListAsync();

        foreach (var r in proximasSalidas)
        {
            var yaNotificado = await context.Notifications.AnyAsync(n =>
                n.UserId == r.UserId &&
                n.Title == "Recordatorio de salida" &&
                n.Message.Contains(r.Id.ToString()));

            if (yaNotificado) continue;

            context.Notifications.Add(new Notification
            {
                UserId    = r.UserId,
                Title     = "Recordatorio de salida",
                Message   = $"[{r.Id}] Tu check-out en {r.Property.Title} es manana {r.CheckOut:dd/MM/yyyy} a las 12:00 PM.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await emailService.SendAsync(
                    r.User.Email!,
                    r.User.FullName,
                    "Recordatorio de salida - RentalHub",
                    $@"<h2>Tu salida es manana</h2>
                    <p>Hola {r.User.FullName},</p>
                    <p>Te recordamos que manana debes hacer check-out de <b>{r.Property.Title}</b>.</p>
                    <table style='border-collapse:collapse;width:100%'>
                        <tr><td style='padding:8px;border:1px solid #ddd'><b>Check-out</b></td>
                            <td style='padding:8px;border:1px solid #ddd'>{r.CheckOut:dd/MM/yyyy} a las 12:00 PM</td></tr>
                        <tr><td style='padding:8px;border:1px solid #ddd'><b>Inmueble</b></td>
                            <td style='padding:8px;border:1px solid #ddd'>{r.Property.Title}</td></tr>
                        <tr><td style='padding:8px;border:1px solid #ddd'><b>Ciudad</b></td>
                            <td style='padding:8px;border:1px solid #ddd'>{r.Property.City}</td></tr>
                    </table>
                    <p>Gracias por usar RentalHub.</p>"
                );
            }
            catch { }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Recordatorios revisados: {llegadas} llegadas, {salidas} salidas",
            proximasLlegadas.Count, proximasSalidas.Count);
    }
}
