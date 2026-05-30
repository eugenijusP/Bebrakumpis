namespace Bebrakumpis.Application.Features.Houses.DTOs;

public class HouseResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BookingColor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
