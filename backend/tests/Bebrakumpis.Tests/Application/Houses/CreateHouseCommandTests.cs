using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.Commands;
using Bebrakumpis.Application.Features.Houses.Validators;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Houses;

public class CreateHouseCommandTests
{
    private readonly Mock<IHouseRepository> _repoMock = new();
    private readonly CreateHouseCommandHandler _handler;

    public CreateHouseCommandTests()
    {
        _handler = new CreateHouseCommandHandler(_repoMock.Object, new CreateHouseCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnHouseResponse_WhenCommandIsValid()
    {
        _repoMock.Setup(r => r.ExistsAsync("Namas 1", default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<House>(), default)).ReturnsAsync(Guid.NewGuid());

        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", null, null, []), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Namas 1", result.Value.Name);
        Assert.Equal("#3b82f6", result.Value.BookingColor);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenNameAlreadyExists()
    {
        _repoMock.Setup(r => r.ExistsAsync("Namas 1", default)).ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", null, null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<House>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenNameIsEmpty()
    {
        var result = await _handler.HandleAsync(
            new CreateHouseCommand("", "#3b82f6", null, null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<House>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenColorIsInvalidHex()
    {
        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "not-a-color", null, null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapRichFields_WhenRichDataIsProvided()
    {
        _repoMock.Setup(r => r.ExistsAsync("Namas 1", default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<House>(), default)).ReturnsAsync(Guid.NewGuid());

        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", "A cosy cabin", "https://example.com/photo.jpg", ["Lake view", "3 bedrooms"]), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("A cosy cabin", result.Value.Description);
        Assert.Equal("https://example.com/photo.jpg", result.Value.PhotoUrl);
        Assert.Equal(["Lake view", "3 bedrooms"], result.Value.Amenities);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenDescriptionExceedsMaxLength()
    {
        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", new string('x', 2001), null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenAmenityIsEmpty()
    {
        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", null, null, ["Lake view", ""]), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenAmenityExceedsMaxLength()
    {
        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", null, null, [new string('x', 101)]), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenAmenitiesExceedMaxCount()
    {
        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", null, null, Enumerable.Range(1, 11).Select(i => $"Amenity {i}").ToList()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenPhotoUrlHasNoScheme()
    {
        var result = await _handler.HandleAsync(
            new CreateHouseCommand("Namas 1", "#3b82f6", null, "not-a-url", []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }
}
