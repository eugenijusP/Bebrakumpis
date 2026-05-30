using System.Data;
using System.Text.Json;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Bebrakumpis.Infrastructure.Persistence;
using Dapper;

namespace Bebrakumpis.Infrastructure.Persistence.Repositories;

public class HouseRepository(IDbConnectionFactory connectionFactory) : IHouseRepository
{
    private static List<string> DeserializeAmenities(string? json) =>
        string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];

    private static string SerializeAmenities(List<string> amenities) =>
        JsonSerializer.Serialize(amenities);

    public async Task<IEnumerable<House>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, name, booking_color, description, photo_url, amenities, created_at
            FROM houses
            ORDER BY created_at
            """, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<HouseRow>(cmd);
        return rows.Select(ToHouse);
    }

    public async Task<House?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, name, booking_color, description, photo_url, amenities, created_at
            FROM houses
            WHERE id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<HouseRow>(cmd);
        return row is null ? null : ToHouse(row);
    }

    public async Task<Guid> CreateAsync(House house, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            INSERT INTO houses (id, name, booking_color, description, photo_url, amenities, created_at)
            VALUES (@Id, @Name, @BookingColor, @Description, @PhotoUrl, @Amenities, @CreatedAt)
            """, ToParams(house), cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
        return house.Id;
    }

    public async Task UpdateAsync(House house, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            UPDATE houses
            SET name = @Name, booking_color = @BookingColor,
                description = @Description, photo_url = @PhotoUrl, amenities = @Amenities
            WHERE id = @Id
            """, ToParams(house), cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            DELETE FROM houses
            WHERE id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT COUNT(1) FROM houses WHERE name = @Name
            """, new { Name = name }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(cmd) > 0;
    }

    public async Task<bool> ExistsForOtherAsync(string name, Guid excludeId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT COUNT(1) FROM houses WHERE name = @Name AND id != @ExcludeId
            """, new { Name = name, ExcludeId = excludeId }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(cmd) > 0;
    }

    public async Task<bool> HasBookingsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT COUNT(1) FROM bookings WHERE house_id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(cmd) > 0;
    }

    private static House ToHouse(HouseRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        BookingColor = row.BookingColor,
        Description = row.Description,
        PhotoUrl = row.PhotoUrl,
        Amenities = DeserializeAmenities(row.Amenities),
        CreatedAt = row.CreatedAt
    };

    private static object ToParams(House house) => new
    {
        house.Id,
        house.Name,
        house.BookingColor,
        house.Description,
        house.PhotoUrl,
        Amenities = SerializeAmenities(house.Amenities),
        house.CreatedAt
    };

    private class HouseRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BookingColor { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Amenities { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
