using System.Data;
using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Infrastructure.Persistence;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bebrakumpis.Tests;

public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString();

    public override Guid Parse(object value)
        => Guid.Parse((string)value);
}

public class TestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid():N}";
    private SqliteConnection? _keepAlive;

    public async Task InitializeAsync()
    {
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        _keepAlive = new SqliteConnection($"DataSource={_dbName};Mode=Memory;Cache=Shared");
        await _keepAlive.OpenAsync();
        _keepAlive.CreateFunction("CONCAT", (string? a, string? b, string? c) => string.Concat(a, b, c));
        await _keepAlive.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS users (
                id            TEXT    NOT NULL PRIMARY KEY,
                first_name    TEXT    NOT NULL DEFAULT '',
                last_name     TEXT    NOT NULL DEFAULT '',
                username      TEXT    NOT NULL UNIQUE,
                password_hash TEXT    NOT NULL,
                role          TEXT    NOT NULL DEFAULT 'User',
                is_active     INTEGER NOT NULL DEFAULT 1,
                created_at    TEXT    NOT NULL DEFAULT (datetime('now'))
            );
            CREATE TABLE IF NOT EXISTS houses (
                id             TEXT NOT NULL PRIMARY KEY,
                name           TEXT NOT NULL UNIQUE,
                booking_color  TEXT NOT NULL DEFAULT '#3b82f6',
                created_at     TEXT NOT NULL DEFAULT (datetime('now'))
            );
            CREATE TABLE IF NOT EXISTS bookings (
                id           TEXT    NOT NULL PRIMARY KEY,
                house_id     TEXT    NOT NULL REFERENCES houses(id),
                type         TEXT    NOT NULL,
                start_date   TEXT    NOT NULL,
                end_date     TEXT    NOT NULL,
                display_text TEXT    NOT NULL,
                notes        TEXT    NULL,
                created_by   TEXT    NOT NULL REFERENCES users(id),
                created_at   TEXT    NOT NULL DEFAULT (datetime('now'))
            );
            CREATE TABLE IF NOT EXISTS pictures (
                id          TEXT    NOT NULL PRIMARY KEY,
                blob_url    TEXT    NOT NULL,
                "order"     INTEGER NOT NULL DEFAULT 0,
                uploaded_at TEXT    NOT NULL DEFAULT (datetime('now'))
            );
            """);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-for-integration-tests-min32chars!",
                ["Jwt:Issuer"] = "Bebrakumpis",
                ["Jwt:Audience"] = "Bebrakumpis"
            });
        });
        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbConnectionFactory));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);
            services.AddScoped<IDbConnectionFactory>(_ =>
                new SqliteConnectionFactory($"DataSource={_dbName};Mode=Memory;Cache=Shared"));

            var blobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorageService));
            if (blobDescriptor is not null) services.Remove(blobDescriptor);
            services.AddScoped<IBlobStorageService, FakeBlobStorageService>();
        });
    }

    public async Task<User> SeedUserAsync(string username = "testuser", string role = "User", string password = "Test@123",
        string firstName = "Test", string lastName = "User", bool isActive = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        await _keepAlive!.ExecuteAsync(
            "INSERT OR IGNORE INTO users (id, first_name, last_name, username, password_hash, role, is_active, created_at) VALUES (@Id, @FirstName, @LastName, @Username, @PasswordHash, @Role, @IsActive, @CreatedAt)",
            user);
        return user;
    }

    public async Task<House> SeedHouseAsync(string name = "Test House", string bookingColor = "#3b82f6")
    {
        var house = new House
        {
            Id = Guid.NewGuid(),
            Name = name,
            BookingColor = bookingColor,
            CreatedAt = DateTime.UtcNow
        };
        await _keepAlive!.ExecuteAsync(
            "INSERT OR IGNORE INTO houses (id, name, booking_color, created_at) VALUES (@Id, @Name, @BookingColor, @CreatedAt)",
            house);
        return house;
    }

    public async Task<Booking> SeedBookingAsync(Guid houseId, Guid createdBy,
        string type = "B", string displayText = "Test booking",
        DateTime? startDate = null, DateTime? endDate = null, string? notes = null)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            HouseId = houseId,
            Type = type,
            StartDate = (startDate ?? DateTime.UtcNow.Date),
            EndDate = (endDate ?? DateTime.UtcNow.Date.AddDays(2)),
            DisplayText = displayText,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        await _keepAlive!.ExecuteAsync(
            "INSERT OR IGNORE INTO bookings (id, house_id, type, start_date, end_date, display_text, notes, created_by, created_at) VALUES (@Id, @HouseId, @Type, @StartDate, @EndDate, @DisplayText, @Notes, @CreatedBy, @CreatedAt)",
            booking);
        return booking;
    }

    public async Task<Picture> SeedPictureAsync(string blobUrl = "https://fake.blob.core.windows.net/gallery/test.jpg",
        int order = 0)
    {
        var picture = new Picture
        {
            Id = Guid.NewGuid(),
            BlobUrl = blobUrl,
            Order = order,
            UploadedAt = DateTime.UtcNow
        };
        await _keepAlive!.ExecuteAsync(
            "INSERT OR IGNORE INTO pictures (id, blob_url, \"order\", uploaded_at) VALUES (@Id, @BlobUrl, @Order, @UploadedAt)",
            picture);
        return picture;
    }

    public new async Task DisposeAsync()
    {
        if (_keepAlive is not null)
            await _keepAlive.DisposeAsync();
        await base.DisposeAsync();
    }
}

public class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqliteConnection(connectionString);
}

public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(Stream content, string contentType, string blobName, CancellationToken cancellationToken = default)
        => Task.FromResult($"https://fake.blob.core.windows.net/gallery/{blobName}");

    public Task DeleteByUrlAsync(string blobUrl, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
