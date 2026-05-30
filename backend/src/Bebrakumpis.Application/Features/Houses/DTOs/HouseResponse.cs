namespace Bebrakumpis.Application.Features.Houses.DTOs;

public class HouseResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BookingColor { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public List<string> Amenities { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
