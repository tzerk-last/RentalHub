namespace RentalHub.Models;

public class Property
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public int MaxGuests { get; set; }

    public int Bedrooms { get; set; }

    public int Bathrooms { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string OwnerId { get; set; } = string.Empty;

    public ICollection<PropertyImage> Images { get; set; }
        = new List<PropertyImage>();

    public ICollection<Reservation> Reservations { get; set; }
        = new List<Reservation>();
}