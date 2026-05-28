using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Bookings.Commands;
using Bebrakumpis.Application.Features.Bookings.Validators;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Bookings;

public class UpdateBookingCommandTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IHouseRepository> _houseRepoMock = new();
    private readonly UpdateBookingCommandHandler _handler;

    public UpdateBookingCommandTests()
    {
        _handler = new UpdateBookingCommandHandler(
            _bookingRepoMock.Object, _houseRepoMock.Object, new UpdateBookingCommandValidator());
    }

    private static Booking MakeBooking(Guid id, Guid houseId) => new()
    {
        Id = id,
        HouseId = houseId,
        Type = "B",
        StartDate = new DateTime(2025, 6, 1),
        EndDate = new DateTime(2025, 6, 7),
        DisplayText = "Old",
        CreatedBy = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow
    };

    private static House MakeHouse(Guid id) => new()
    {
        Id = id, Name = "House A",
        BookingColor = "#3b82f6", ReservedColor = "#ef4444", CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task HandleAsync_ShouldReturnUpdatedResponse_WhenCommandIsValid()
    {
        var bookingId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, default)).ReturnsAsync(MakeBooking(bookingId, houseId));
        _houseRepoMock.Setup(r => r.GetByIdAsync(houseId, default)).ReturnsAsync(MakeHouse(houseId));

        var result = await _handler.HandleAsync(
            new UpdateBookingCommand(bookingId, houseId, "R",
                new DateTime(2025, 6, 5), new DateTime(2025, 6, 10), "Updated", "Some notes"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("R", result.Value.Type);
        Assert.Equal("Updated", result.Value.DisplayText);
        _bookingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        var bookingId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, default)).ReturnsAsync((Booking?)null);

        var result = await _handler.HandleAsync(
            new UpdateBookingCommand(bookingId, Guid.NewGuid(), "B",
                new DateTime(2025, 6, 1), new DateTime(2025, 6, 7), "Test", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _bookingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHouseDoesNotExist()
    {
        var bookingId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, default)).ReturnsAsync(MakeBooking(bookingId, houseId));
        _houseRepoMock.Setup(r => r.GetByIdAsync(houseId, default)).ReturnsAsync((House?)null);

        var result = await _handler.HandleAsync(
            new UpdateBookingCommand(bookingId, houseId, "B",
                new DateTime(2025, 6, 1), new DateTime(2025, 6, 7), "Test", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenTypeIsInvalid()
    {
        var result = await _handler.HandleAsync(
            new UpdateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), "Z",
                new DateTime(2025, 6, 1), new DateTime(2025, 6, 7), "Test", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }
}
