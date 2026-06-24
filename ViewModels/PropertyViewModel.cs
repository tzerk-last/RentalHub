namespace RentalHub.ViewModels;

public class PropertyViewModel
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
    public List<IFormFile>? Images { get; set; }
    public List<RentalHub.Models.PropertyImage> ExistingImages { get; set; } = new();
}
