using System.Data;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Bebrakumpis.Infrastructure.Persistence;
using Dapper;

namespace Bebrakumpis.Infrastructure.Persistence.Repositories;

public class HouseRepository(IDbConnectionFactory connectionFactory) : IHouseRepository
{
    public async Task<IEnumerable<House>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, name, booking_color, created_at
            FROM houses
            ORDER BY created_at
            """, cancellationToken: cancellationToken);
        return await connection.QueryAsync<House>(cmd);
    }

    public async Task<House?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, name, booking_color, created_at
            FROM houses
            WHERE id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<House>(cmd);
    }

    public async Task<Guid> CreateAsync(House house, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            INSERT INTO houses (id, name, booking_color, created_at)
            VALUES (@Id, @Name, @BookingColor, @CreatedAt)
            """, house, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
        return house.Id;
    }

    public async Task UpdateAsync(House house, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            UPDATE houses
            SET name = @Name, booking_color = @BookingColor
            WHERE id = @Id
            """, house, cancellationToken: cancellationToken);
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

    public async Task<bool> HasBookingsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT COUNT(1) FROM bookings WHERE house_id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(cmd) > 0;
    }
}
