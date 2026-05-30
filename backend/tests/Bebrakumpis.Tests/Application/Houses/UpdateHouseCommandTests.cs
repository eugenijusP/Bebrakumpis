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
            new UpdateHouseCommand(id, "New Name", "#3b82f6"), default);

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
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "#3b82f6"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenColorIsInvalidHex()
    {
        var result = await _handler.HandleAsync(
            new UpdateHouseCommand(Guid.NewGuid(), "Name", "bad"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
