using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Bookings.Commands;
using Bebrakumpis.Application.Features.Bookings.Validators;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Bookings;

public class CreateBookingCommandTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IHouseRepository> _houseRepoMock = new();
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingCommandTests()
    {
        _handler = new CreateBookingCommandHandler(
            _bookingRepoMock.Object, _houseRepoMock.Object, new CreateBookingCommandValidator());
    }

    private static House MakeHouse(Guid id) => new()
    {
        Id = id,
        Name = "House A",
        BookingColor = "#3b82f6",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task HandleAsync_ShouldReturnBookingResponse_WhenCommandIsValid()
    {
        var houseId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        _houseRepoMock.Setup(r => r.GetByIdAsync(houseId, default)).ReturnsAsync(MakeHouse(houseId));
        _bookingRepoMock.Setup(r => r.CreateAsync(It.IsAny<Booking>(), default)).ReturnsAsync(Guid.NewGuid());

        var result = await _handler.HandleAsync(
            new CreateBookingCommand(requesterId, houseId, "B",
                new DateTime(2025, 6, 1), new DateTime(2025, 6, 7), "Petersen family", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(houseId, result.Value.HouseId);
        Assert.Equal("B", result.Value.Type);
        Assert.Equal("Petersen family", result.Value.DisplayText);
        _bookingRepoMock.Verify(r => r.CreateAsync(It.IsAny<Booking>(), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHouseDoesNotExist()
    {
        var houseId = Guid.NewGuid();
        _houseRepoMock.Setup(r => r.GetByIdAsync(houseId, default)).ReturnsAsync((House?)null);

        var result = await _handler.HandleAsync(
            new CreateBookingCommand(Guid.NewGuid(), houseId, "B",
                new DateTime(2025, 6, 1), new DateTime(2025, 6, 7), "Test", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _bookingRepoMock.Verify(r => r.CreateAsync(It.IsAny<Booking>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenTypeIsInvalid()
    {
        var result = await _handler.HandleAsync(
            new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), "X",
                new DateTime(2025, 6, 1), new DateTime(2025, 6, 7), "Test", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenDisplayTextExceeds50Chars()
    {
        var result = await _handler.HandleAsync(
            new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), "B",
                new DateTime(2025, 6, 1), new DateTime(2025, 6, 7),
                new string('A', 51), null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenEndDateBeforeStartDate()
    {
        var result = await _handler.HandleAsync(
            new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), "B",
                new DateTime(2025, 6, 7), new DateTime(2025, 6, 1), "Test", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }
}
