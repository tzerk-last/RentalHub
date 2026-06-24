namespace RentalHub.Models;

public class Wishlist
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
}
