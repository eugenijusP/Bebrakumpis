using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.Commands;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Houses;

public class DeleteHouseCommandTests
{
    private readonly Mock<IHouseRepository> _repoMock = new();
    private readonly DeleteHouseCommandHandler _handler;

    public DeleteHouseCommandTests()
    {
        _handler = new DeleteHouseCommandHandler(_repoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteHouse_WhenHouseExistsAndHasNoBookings()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(new House { Id = id, Name = "Namas 1" });
        _repoMock.Setup(r => r.HasBookingsAsync(id, default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(new DeleteHouseCommand(id), default);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.DeleteAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHouseDoesNotExist()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((House?)null);

        var result = await _handler.HandleAsync(new DeleteHouseCommand(Guid.NewGuid()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenHouseHasExistingBookings()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(new House { Id = id, Name = "Namas 1" });
        _repoMock.Setup(r => r.HasBookingsAsync(id, default)).ReturnsAsync(true);

        var result = await _handler.HandleAsync(new DeleteHouseCommand(id), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
