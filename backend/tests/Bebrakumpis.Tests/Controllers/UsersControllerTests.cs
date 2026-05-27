using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bebrakumpis.Tests.Controllers;

public class UsersControllerTests : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _anonClient;
    private HttpClient? _adminClient;
    private HttpClient? _userClient;

    public UsersControllerTests(TestWebAppFactory factory)
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
        await _factory.SeedUserAsync("useradmin", "Admin", "Test@123");
        _adminClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await _adminClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "useradmin", password = "Test@123" });

        await _factory.SeedUserAsync("regularuser", "User", "Test@123");
        _userClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await _userClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "regularuser", password = "Test@123" });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_ShouldReturn200_WhenCalledByAdmin()
    {
        var response = await _adminClient!.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetAll_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _anonClient.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var response = await _userClient!.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenAdminCreatesUser()
    {
        var response = await _adminClient!.PostAsJsonAsync("/api/v1/users",
            new { firstName = "Jane", lastName = "Doe", username = "janedoe", password = "Secret1", role = "User" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("janedoe", body.GetProperty("username").GetString());
        Assert.Equal("Jane", body.GetProperty("firstName").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenUsernameAlreadyExists()
    {
        await _factory.SeedUserAsync("duplicateuser", "User", "Test@123");

        var response = await _adminClient!.PostAsJsonAsync("/api/v1/users",
            new { firstName = "Dup", lastName = "User", username = "duplicateuser", password = "Secret1", role = "User" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenRoleIsInvalid()
    {
        var response = await _adminClient!.PostAsJsonAsync("/api/v1/users",
            new { firstName = "Bad", lastName = "Role", username = "badrole", password = "Secret1", role = "SuperAdmin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/v1/users",
            new { firstName = "Anon", lastName = "User", username = "anonuser", password = "Secret1", role = "User" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var response = await _userClient!.PostAsJsonAsync("/api/v1/users",
            new { firstName = "Forbidden", lastName = "User", username = "forbiddenuser", password = "Secret1", role = "User" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WhenAdminUpdatesUser()
    {
        var user = await _factory.SeedUserAsync("updateme", "User", "Test@123", "Old", "Name");

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/users/{user.Id}",
            new { firstName = "New", lastName = "Name", role = "Admin", isActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New", body.GetProperty("firstName").GetString());
        Assert.Equal("Admin", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var user = await _factory.SeedUserAsync("updateforbidden", "User", "Test@123");

        var response = await _userClient!.PutAsJsonAsync($"/api/v1/users/{user.Id}",
            new { firstName = "Hacker", lastName = "User", role = "Admin", isActive = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenUserDoesNotExist()
    {
        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}",
            new { firstName = "Ghost", lastName = "User", role = "User", isActive = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn409_WhenAdminTriesToDeactivateThemselves()
    {
        var admin = await _factory.SeedUserAsync("selfdeactivate", "Admin", "Test@123");
        var selfClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await selfClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "selfdeactivate", password = "Test@123" });

        var response = await selfClient.PutAsJsonAsync($"/api/v1/users/{admin.Id}",
            new { firstName = "Self", lastName = "Admin", role = "Admin", isActive = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturn204_WhenUserChangesOwnPassword()
    {
        var user = await _factory.SeedUserAsync("pwchange", "User", "Test@123");
        var pwClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await pwClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "pwchange", password = "Test@123" });

        var response = await pwClient.PutAsJsonAsync($"/api/v1/users/{user.Id}/password",
            new { currentPassword = "Test@123", newPassword = "NewPass1" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturn400_WhenCurrentPasswordIsWrong()
    {
        var user = await _factory.SeedUserAsync("pwwrong", "User", "Test@123");
        var pwClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await pwClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "pwwrong", password = "Test@123" });

        var response = await pwClient.PutAsJsonAsync($"/api/v1/users/{user.Id}/password",
            new { currentPassword = "WrongPassword", newPassword = "NewPass1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturn403_WhenNonAdminTriesToChangeOtherUsersPassword()
    {
        var other = await _factory.SeedUserAsync("otheruser2", "User", "Test@123");

        var response = await _userClient!.PutAsJsonAsync($"/api/v1/users/{other.Id}/password",
            new { newPassword = "NewPass1" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _anonClient.PutAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}/password",
            new { newPassword = "NewPass1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturn204_WhenAdminChangesOtherUsersPassword()
    {
        var target = await _factory.SeedUserAsync("targetpw", "User", "Test@123");

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/users/{target.Id}/password",
            new { newPassword = "AdminSet1" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenUserIsDeactivated()
    {
        await _factory.SeedUserAsync("inactive", "User", "Test@123", isActive: false);

        var response = await _anonClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "inactive", password = "Test@123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
