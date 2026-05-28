namespace Bebrakumpis.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid HouseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    // Populated via JOIN in GetByMonthAsync — not stored in the bookings table
    public string? CreatedByName { get; set; }
}
