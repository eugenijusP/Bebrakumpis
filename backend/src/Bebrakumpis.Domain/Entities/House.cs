namespace Bebrakumpis.Domain.Entities;

public class House
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BookingColor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
