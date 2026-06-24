using RentalHub.Models;

namespace RentalHub.Models;

public class Reservation
{
    public int Id { get; set; }

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    public string Status { get; set; } = string.Empty;

    public int PropertyId { get; set; }

    public Property Property { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;
}