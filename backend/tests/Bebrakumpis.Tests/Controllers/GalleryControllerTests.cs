using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bebrakumpis.Tests.Controllers;

public class GalleryControllerTests : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _anonClient;
    private HttpClient? _adminClient;
    private HttpClient? _userClient;

    public GalleryControllerTests(TestWebAppFactory factory)
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
        await _factory.SeedUserAsync("galleryadmin", "Admin", "Test@123");
        _adminClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await _adminClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "galleryadmin", password = "Test@123" });

        await _factory.SeedUserAsync("galleryuser", "User", "Test@123");
        _userClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await _userClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "galleryuser", password = "Test@123" });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_ShouldReturn200_WhenCalledAnonymously()
    {
        var response = await _anonClient.GetAsync("/api/v1/gallery");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnPicturesOrderedByOrder_WhenPicturesExist()
    {
        await _factory.SeedPictureAsync("https://fake.blob/second.jpg", order: 10);
        await _factory.SeedPictureAsync("https://fake.blob/first.jpg", order: 5);

        var response = await _anonClient.GetAsync("/api/v1/gallery");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = body.EnumerateArray().Select(p => p.GetProperty("order").GetInt32()).ToList();
        Assert.True(orders.Count >= 2);
        Assert.Equal(orders.OrderBy(o => o).ToList(), orders);
    }

    [Fact]
    public async Task Upload_ShouldReturn201_WhenAdminUploadsJpeg()
    {
        var bytes = new byte[100];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        var response = await _adminClient!.PostAsync("/api/v1/gallery", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("fake.blob", body.GetProperty("blobUrl").GetString());
    }

    [Fact]
    public async Task Upload_ShouldReturn400_WhenFileTypeIsInvalid()
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(new byte[100]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "doc.pdf");

        var response = await _adminClient!.PostAsync("/api/v1/gallery", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ShouldReturn403_WhenCalledByNonAdminUser()
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(new byte[100]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        var response = await _userClient!.PostAsync("/api/v1/gallery", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ShouldReturn401_WhenNotAuthenticated()
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(new byte[100]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        var response = await _anonClient.PostAsync("/api/v1/gallery", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrder_ShouldReturn200_WhenAdminUpdatesOrder()
    {
        var picture = await _factory.SeedPictureAsync("https://fake.blob/reorder.jpg", order: 1);

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/gallery/{picture.Id}",
            new { order = 5 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5, body.GetProperty("order").GetInt32());
    }

    [Fact]
    public async Task UpdateOrder_ShouldReturn404_WhenPictureDoesNotExist()
    {
        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/gallery/{Guid.NewGuid()}",
            new { order = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrder_ShouldReturn400_WhenOrderIsNegative()
    {
        var picture = await _factory.SeedPictureAsync("https://fake.blob/neg.jpg", order: 1);

        var response = await _adminClient!.PutAsJsonAsync($"/api/v1/gallery/{picture.Id}",
            new { order = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrder_ShouldReturn403_WhenCalledByNonAdminUser()
    {
        var picture = await _factory.SeedPictureAsync("https://fake.blob/forbidden.jpg", order: 1);

        var response = await _userClient!.PutAsJsonAsync($"/api/v1/gallery/{picture.Id}",
            new { order = 2 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrder_ShouldReturn401_WhenNotAuthenticated()
    {
        var picture = await _factory.SeedPictureAsync("https://fake.blob/auth.jpg", order: 1);

        var response = await _anonClient.PutAsJsonAsync($"/api/v1/gallery/{picture.Id}",
            new { order = 2 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenAdminDeletesPicture()
    {
        var picture = await _factory.SeedPictureAsync("https://fake.blob/delete.jpg", order: 1);

        var response = await _adminClient!.DeleteAsync($"/api/v1/gallery/{picture.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenPictureDoesNotExist()
    {
        var response = await _adminClient!.DeleteAsync($"/api/v1/gallery/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn403_WhenCalledByNonAdminUser()
    {
        var picture = await _factory.SeedPictureAsync("https://fake.blob/forbiddendelete.jpg", order: 1);

        var response = await _userClient!.DeleteAsync($"/api/v1/gallery/{picture.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn401_WhenNotAuthenticated()
    {
        var picture = await _factory.SeedPictureAsync("https://fake.blob/authdelete.jpg", order: 1);

        var response = await _anonClient.DeleteAsync($"/api/v1/gallery/{picture.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
