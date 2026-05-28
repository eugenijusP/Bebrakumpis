namespace Bebrakumpis.Application.Features.Bookings.DTOs;

public class UpdateBookingRequest
{
    public Guid HouseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
