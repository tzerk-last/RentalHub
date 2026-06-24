namespace RentalHub.ViewModels;

public class DashboardViewModel
{
    public int TotalProperties { get; set; }
    public int TotalReservations { get; set; }
    public int PendingReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<RecentReservationItem> RecentReservations { get; set; } = new();
}

public class RecentReservationItem
{
    public string GuestName { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
}
