using System.Data;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Bebrakumpis.Infrastructure.Persistence;
using Dapper;

namespace Bebrakumpis.Infrastructure.Persistence.Repositories;

public class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, first_name, last_name, username, password_hash, role, is_active, created_at
            FROM users
            ORDER BY created_at
            """, cancellationToken: cancellationToken);
        return await connection.QueryAsync<User>(cmd);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, first_name, last_name, username, password_hash, role, is_active, created_at
            FROM users
            WHERE id = @Id
            """, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<User>(cmd);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT id, first_name, last_name, username, password_hash, role, is_active, created_at
            FROM users
            WHERE username = @Username
            """, new { Username = username }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<User>(cmd);
    }

    public async Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            SELECT COUNT(1) FROM users WHERE username = @Username
            """, new { Username = username }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(cmd) > 0;
    }

    public async Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            INSERT INTO users (id, first_name, last_name, username, password_hash, role, is_active, created_at)
            VALUES (@Id, @FirstName, @LastName, @Username, @PasswordHash, @Role, @IsActive, @CreatedAt)
            """, user, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
        return user.Id;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            UPDATE users
            SET first_name = @FirstName, last_name = @LastName, role = @Role, is_active = @IsActive
            WHERE id = @Id
            """, user, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }

    public async Task ChangePasswordAsync(Guid id, string passwordHash, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        var cmd = new CommandDefinition("""
            UPDATE users SET password_hash = @PasswordHash WHERE id = @Id
            """, new { Id = id, PasswordHash = passwordHash }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }
}
