using System.Data;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Bebrakumpis.Infrastructure.Persistence;
using Dapper;

namespace Bebrakumpis.Infrastructure.Persistence.Repositories;

public class BookingRepository(IDbConnectionFactory connectionFactory) : IBookingRepository
{
    public async Task<IEnumerable<Booking>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var firstDay = new DateTime(year, month, 1).Date;
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;

        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT b.id, b.house_id, b.type, b.start_date, b.end_date,
                   b.display_text, b.notes, b.created_by, b.created_at,
                   CONCAT(u.first_name, ' ', u.last_name) AS created_by_name
            FROM bookings b
            LEFT JOIN users u ON u.id = b.created_by
            WHERE b.start_date <= @LastDay AND b.end_date >= @FirstDay
            ORDER BY b.start_date
            """, new { FirstDay = firstDay, LastDay = lastDay }, cancellationToken: cancellationToken);
        return await connection.QueryAsync<Booking>(cmd);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, house_id, type, start_date, end_date,
                   display_text, notes, created_by, created_at
            FROM bookings
            WHERE id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Booking>(cmd);
    }

    public async Task<Guid> CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            INSERT INTO bookings (id, house_id, type, start_date, end_date, display_text, notes, created_by, created_at)
            VALUES (@Id, @HouseId, @Type, @StartDate, @EndDate, @DisplayText, @Notes, @CreatedBy, @CreatedAt)
            """, booking, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
        return booking.Id;
    }

    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            UPDATE bookings
            SET house_id = @HouseId, type = @Type, start_date = @StartDate, end_date = @EndDate,
                display_text = @DisplayText, notes = @Notes
            WHERE id = @Id
            """, booking, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            DELETE FROM bookings WHERE id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }


}
