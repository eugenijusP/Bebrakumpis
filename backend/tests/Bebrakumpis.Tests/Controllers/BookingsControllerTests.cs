using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bebrakumpis.Tests.Controllers;

public class BookingsControllerTests : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _anonClient;
    private HttpClient? _adminClient;
    private HttpClient? _userClient;

    public BookingsControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _anonClient = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedUserAsync("bookingadmin", "Admin", "Test@123", "Admin", "User");
        _adminClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await _adminClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "bookingadmin", password = "Test@123" });

        await _factory.SeedUserAsync("bookinguser", "User", "Test@123", "Regular", "User");
        _userClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await _userClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "bookinguser", password = "Test@123" });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetByMonth_ShouldReturn200_WhenCalledAnonymously()
    {
        var response = await _anonClient.GetAsync("/api/v1/bookings?year=2025&month=6");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByMonth_ShouldFilterSensitiveFields_ForGuest()
    {
        var house = await _factory.SeedHouseAsync("GuestHouse");
        var admin = await _factory.SeedUserAsync("guestbooker", "Admin", "Test@123");
        await _factory.SeedBookingAsync(house.Id, admin.Id, "B", "Guest view",
            new DateTime(2025, 7, 1), new DateTime(2025, 7, 5), "Secret notes");

        var response = await _anonClient.GetAsync("/api/v1/bookings?year=2025&month=7");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = body.EnumerateArray().First(b => b.GetProperty("displayText").GetString() == "Guest view");
        Assert.Equal(JsonValueKind.Null, item.GetProperty("notes").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("createdByName").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("createdAt").ValueKind);
    }

    [Fact]
    public async Task GetByMonth_ShouldIncludeNotesAndCreatedByName_ForAuthenticatedUser()
    {
        var house = await _factory.SeedHouseAsync("UserHouse");
        var admin = await _factory.SeedUserAsync("userbooker", "Admin", "Test@123", "John", "Smith");
        await _factory.SeedBookingAsync(house.Id, admin.Id, "R", "User view",
            new DateTime(2025, 8, 1), new DateTime(2025, 8, 3), "User notes");

        var response = await _userClient!.GetAsync("/api/v1/bookings?year=2025&month=8");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = body.EnumerateArray().First(b => b.GetProperty("displayText").GetString() == "User view");
        Assert.Equal("User notes", item.GetProperty("notes").GetString());
        Assert.Equal("John Smith", item.GetProperty("createdByName").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("createdAt").ValueKind);
    }

    [Fact]
    public async Task GetByMonth_ShouldIncludeCreatedAt_ForAdmin()
    {
        var house = await _factory.SeedHouseAsync("AdminHouse");
        var admin = await _factory.SeedUserAsync("adminbooker2", "Admin", "Test@123", "Jane", "Doe");
        await _factory.SeedBookingAsync(house.Id, admin.Id, "B", "Admin view",
            new DateTime(2025, 9, 1), new DateTime(2025, 9, 5));

        var response = await _adminClient!.GetAsync("/api/v1/bookings?year=2025&month=9");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = body.EnumerateArray().First(b => b.GetProperty("displayText").GetString() == "Admin view");
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("createdAt").ValueKind);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenAdminCreatesBooking()
    {
        var house = await _factory.SeedHouseAsync("NewBookingHouse");

        var response = await _adminClient!.PostAsJsonAsync("/api/v1/bookings", new
        {
            houseId = house.Id,
            type = "B",
            startDate = "2025-10-01",
            endDate = "2025-10-07",
            displayText = "Autumn stay"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("B", body.GetProperty("type").GetString());
        Assert.Equal("Autumn stay", body.GetProperty("displayText").GetString());
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenTypeIsInvalid()
    {
        var house = await _factory.SeedHouseAsync("BadTypeHouse");

        var response = await _adminClient!.PostAsJsonAsync("/api/v1/bookings", new
        {
            houseId = house.Id,
            type = "X",
            startDate = "2025-10-01",
            endDate = "2025-10-07",
            displayText = "Bad type"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenDisplayTextExceeds50Chars()
    {
        var house = await _factory.SeedHouseAsync("LongTextHouse");

        var response = await _adminClient!.PostAsJsonAsync("/api/v1/bookings", new
        {
            houseId = house.Id,
            type = "B",
            startDate = "2025-10-01",
            endDate = "2025-10-07",
            displayText = new string('A', 51)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn404_WhenHouseDoesNotExist()
    {
        var response = await _adminClient!.PostAsJsonAsync("/api/v1/bookings", new
        {
            houseId = Guid.NewGuid(),
            type = "B",
            startDate = "2025-10-01",
            endDate = "2025-10-07",
            displayText = "Ghost house"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn401_WhenNotAuthenticated()
    {
        var house = await _factory.SeedHouseAsync("AnonBookHouse");

        var response = await _anonClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            houseId = house.Id,
            type = "B",
            startDate = "2025-10-01",
            endDate = "2025-10-07",
            displayText = "Anon"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var house = await _factory.SeedHouseAsync("ForbiddenBookHouse");

        var response = await _userClient!.PostAsJsonAsync("/api/v1/bookings", new
        {
            houseId = house.Id,
            type = "B",
            startDate = "2025-10-01",
            endDate = "2025-10-07",
            displayText = "Forbidden"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WhenAdminUpdatesBooking()
    {
        var house = await _factory.SeedHouseAsync("UpdateBookHouse");
        var admin = await _factory.SeedUserAsync("updatebookadmin", "Admin", "Test@123");
        var booking = await _factory.SeedBookingAsync(house.Id, admin.Id, "B", "Before update",
            new DateTime(2025, 11, 1), new DateTime(2025, 11, 5));

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/bookings/{booking.Id}", new
        {
            houseId = house.Id,
            type = "R",
            startDate = "2025-11-02",
            endDate = "2025-11-06",
            displayText = "After update"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("R", body.GetProperty("type").GetString());
        Assert.Equal("After update", body.GetProperty("displayText").GetString());
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenBookingDoesNotExist()
    {
        var house = await _factory.SeedHouseAsync("UpdateMissingHouse");

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/bookings/{Guid.NewGuid()}", new
        {
            houseId = house.Id,
            type = "B",
            startDate = "2025-11-01",
            endDate = "2025-11-05",
            displayText = "Ghost"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _anonClient.PutAsJsonAsync($"/api/v1/bookings/{Guid.NewGuid()}", new
        {
            houseId = Guid.NewGuid(),
            type = "B",
            startDate = "2025-11-01",
            endDate = "2025-11-05",
            displayText = "Anon update"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var house = await _factory.SeedHouseAsync("UpdateForbiddenHouse");
        var admin = await _factory.SeedUserAsync("updateforbiddenadmin", "Admin", "Test@123");
        var booking = await _factory.SeedBookingAsync(house.Id, admin.Id);

        var response = await _userClient!.PutAsJsonAsync($"/api/v1/bookings/{booking.Id}", new
        {
            houseId = house.Id,
            type = "B",
            startDate = "2025-11-01",
            endDate = "2025-11-05",
            displayText = "Forbidden"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenAdminDeletesBooking()
    {
        var house = await _factory.SeedHouseAsync("DeleteBookHouse");
        var admin = await _factory.SeedUserAsync("deletebookadmin", "Admin", "Test@123");
        var booking = await _factory.SeedBookingAsync(house.Id, admin.Id);

        var response = await _adminClient!.DeleteAsync($"/api/v1/bookings/{booking.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenBookingDoesNotExist()
    {
        var response = await _adminClient!.DeleteAsync($"/api/v1/bookings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _anonClient.DeleteAsync($"/api/v1/bookings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var house = await _factory.SeedHouseAsync("DeleteForbiddenHouse");
        var admin = await _factory.SeedUserAsync("deleteforbiddenadmin", "Admin", "Test@123");
        var booking = await _factory.SeedBookingAsync(house.Id, admin.Id);

        var response = await _userClient!.DeleteAsync($"/api/v1/bookings/{booking.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteHouse_ShouldReturn409_WhenHouseHasBookings()
    {
        var house = await _factory.SeedHouseAsync("HouseWithBookings");
        var admin = await _factory.SeedUserAsync("housebookingadmin", "Admin", "Test@123");
        await _factory.SeedBookingAsync(house.Id, admin.Id);

        var response = await _adminClient!.DeleteAsync($"/api/v1/houses/{house.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
