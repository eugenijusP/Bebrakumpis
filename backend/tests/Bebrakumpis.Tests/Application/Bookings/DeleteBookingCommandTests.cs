using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Bookings.Commands;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Bookings;

public class DeleteBookingCommandTests
{
    private readonly Mock<IBookingRepository> _repoMock = new();
    private readonly DeleteBookingCommandHandler _handler;

    public DeleteBookingCommandTests()
    {
        _handler = new DeleteBookingCommandHandler(_repoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldDelete_WhenBookingExists()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(new Booking { Id = id });

        var result = await _handler.HandleAsync(new DeleteBookingCommand(id), default);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.DeleteAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Booking?)null);

        var result = await _handler.HandleAsync(new DeleteBookingCommand(id), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
