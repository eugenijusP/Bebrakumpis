using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bebrakumpis.Tests.Controllers;

public class HousesControllerTests : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _anonClient;
    private HttpClient? _adminClient;

    public HousesControllerTests(TestWebAppFactory factory)
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
        await _factory.SeedUserAsync("houseadmin", "Admin", "Test@123");
        _adminClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await _adminClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "houseadmin", password = "Test@123" });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_ShouldReturn200_WhenCalledAnonymously()
    {
        await _factory.SeedHouseAsync("Public House");

        var response = await _anonClient.GetAsync("/api/v1/houses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnHouseList_WhenHousesExist()
    {
        await _factory.SeedHouseAsync("Listed House");

        var response = await _anonClient.GetAsync("/api/v1/houses");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetById_ShouldReturn200_WhenHouseExists()
    {
        var house = await _factory.SeedHouseAsync("GetById House");

        var response = await _anonClient.GetAsync($"/api/v1/houses/{house.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("GetById House", body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenHouseDoesNotExist()
    {
        var response = await _anonClient.GetAsync($"/api/v1/houses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenAdminCreatesHouse()
    {
        var response = await _adminClient!.PostAsJsonAsync("/api/v1/houses",
            new { name = "New House", bookingColor = "#1d4ed8", reservedColor = "#dc2626" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New House", body.GetProperty("name").GetString());
        Assert.Equal("#1d4ed8", body.GetProperty("bookingColor").GetString());
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenNameAlreadyExists()
    {
        await _factory.SeedHouseAsync("Duplicate House");

        var response = await _adminClient!.PostAsJsonAsync("/api/v1/houses",
            new { name = "Duplicate House", bookingColor = "#3b82f6", reservedColor = "#ef4444" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenColorIsInvalid()
    {
        var response = await _adminClient!.PostAsJsonAsync("/api/v1/houses",
            new { name = "Bad Color House", bookingColor = "not-a-color", reservedColor = "#dc2626" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/v1/houses",
            new { name = "Unauth House", bookingColor = "#3b82f6", reservedColor = "#ef4444" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenCalledByNonAdminUser()
    {
        await _factory.SeedUserAsync("houseuser", "User", "Test@123");
        var userClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await userClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "houseuser", password = "Test@123" });

        var response = await userClient.PostAsJsonAsync("/api/v1/houses",
            new { name = "Forbidden House", bookingColor = "#3b82f6", reservedColor = "#ef4444" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WhenAdminUpdatesHouse()
    {
        var house = await _factory.SeedHouseAsync("Update Me");

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/houses/{house.Id}",
            new { name = "Updated Name", bookingColor = "#7c3aed", reservedColor = "#059669" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Name", body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenHouseDoesNotExist()
    {
        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/houses/{Guid.NewGuid()}",
            new { name = "Ghost", bookingColor = "#3b82f6", reservedColor = "#ef4444" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenAdminDeletesHouse()
    {
        var house = await _factory.SeedHouseAsync("Delete Me");

        var response = await _adminClient!.DeleteAsync($"/api/v1/houses/{house.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenHouseDoesNotExist()
    {
        var response = await _adminClient!.DeleteAsync($"/api/v1/houses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn401_WhenNotAuthenticated()
    {
        var house = await _factory.SeedHouseAsync("Auth Delete House");

        var response = await _anonClient.DeleteAsync($"/api/v1/houses/{house.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldRoundTripRichFields_WhenUpdatedWithDescriptionPhotoAndAmenities()
    {
        var house = await _factory.SeedHouseAsync("Rich House");

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/houses/{house.Id}", new
        {
            name = "Rich House",
            bookingColor = "#3b82f6",
            description = "A cosy lakeside cabin",
            photoUrl = "https://example.com/photo.jpg",
            amenities = new[] { "Lake view", "3 bedrooms" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("A cosy lakeside cabin", body.GetProperty("description").GetString());
        Assert.Equal("https://example.com/photo.jpg", body.GetProperty("photoUrl").GetString());
        var amenities = body.GetProperty("amenities").EnumerateArray().Select(a => a.GetString()!).ToArray();
        Assert.Equal(["Lake view", "3 bedrooms"], amenities);
    }

    [Fact]
    public async Task Update_ShouldReturn409_WhenRenamedToExistingHouseName()
    {
        await _factory.SeedHouseAsync("Occupied Name");
        var house = await _factory.SeedHouseAsync("Free Name");

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/houses/{house.Id}",
            new { name = "Occupied Name", bookingColor = "#3b82f6" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnEmptyAmenities_WhenAmenitiesColumnIsNull()
    {
        var house = await _factory.SeedLegacyHouseAsync("Null Amenities House");

        var response = await _anonClient.GetAsync($"/api/v1/houses/{house.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Null(body.GetProperty("description").GetString());
        Assert.Null(body.GetProperty("photoUrl").GetString());
        Assert.Equal(0, body.GetProperty("amenities").GetArrayLength());
    }
}
