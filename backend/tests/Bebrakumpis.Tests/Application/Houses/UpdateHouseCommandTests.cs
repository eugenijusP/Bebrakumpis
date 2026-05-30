using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.Commands;
using Bebrakumpis.Application.Features.Houses.Validators;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Houses;

public class UpdateHouseCommandTests
{
    private readonly Mock<IHouseRepository> _repoMock = new();
    private readonly UpdateHouseCommandHandler _handler;

    public UpdateHouseCommandTests()
    {
        _handler = new UpdateHouseCommandHandler(_repoMock.Object, new UpdateHouseCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUpdatedHouse_WhenCommandIsValid()
    {
        var id = Guid.NewGuid();
        var house = new House { Id = id, Name = "Old Name", BookingColor = "#000000", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(house);

        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(id, "New Name", "#3b82f6", null, null, []), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value.Name);
        Assert.Equal("#3b82f6", result.Value.BookingColor);
        _repoMock.Verify(r => r.UpdateAsync(house, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHouseDoesNotExist()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((House?)null);

        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "#3b82f6", null, null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenColorIsInvalidHex()
    {
        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "bad", null, null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapRichFields_WhenRichDataIsProvided()
    {
        var id = Guid.NewGuid();
        var house = new House { Id = id, Name = "Old Name", BookingColor = "#000000", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(house);

        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(id, "New Name", "#3b82f6", "A cosy cabin", "https://example.com/photo.jpg", ["Lake view", "3 bedrooms"]), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("A cosy cabin", result.Value.Description);
        Assert.Equal("https://example.com/photo.jpg", result.Value.PhotoUrl);
        Assert.Equal(["Lake view", "3 bedrooms"], result.Value.Amenities);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenDescriptionExceedsMaxLength()
    {
        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "#3b82f6", new string('x', 2001), null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenAmenityIsEmpty()
    {
        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "#3b82f6", null, null, ["Lake view", ""]), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenAmenityExceedsMaxLength()
    {
        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "#3b82f6", null, null, [new string('x', 101)]), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenAmenitiesExceedMaxCount()
    {
        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "#3b82f6", null, null, Enumerable.Range(1, 11).Select(i => $"Amenity {i}").ToList()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenPhotoUrlHasNoScheme()
    {
        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "#3b82f6", null, "not-a-url", []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenRenamedToExistingHouseName()
    {
        var id = Guid.NewGuid();
        var house = new House { Id = id, Name = "Original Name", BookingColor = "#000000", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(house);
        _repoMock.Setup(r => r.ExistsForOtherAsync("Taken Name", id, default)).ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(id, "Taken Name", "#3b82f6", null, null, []), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<House>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenNameIsUnchanged()
    {
        var id = Guid.NewGuid();
        var house = new House { Id = id, Name = "Same Name", BookingColor = "#000000", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(house);

        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(id, "Same Name", "#3b82f6", null, null, []), default);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.ExistsForOtherAsync(It.IsAny<string>(), It.IsAny<Guid>(), default), Times.Never);
    }
}
