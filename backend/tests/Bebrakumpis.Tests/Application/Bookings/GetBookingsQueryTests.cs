using Bebrakumpis.Application.Features.Bookings.Queries;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Bookings;

public class GetBookingsQueryTests
{
    private readonly Mock<IBookingRepository> _repoMock = new();
    private readonly GetBookingsQueryHandler _handler;

    public GetBookingsQueryTests()
    {
        _handler = new GetBookingsQueryHandler(_repoMock.Object);
    }

    private static Booking MakeBooking() => new()
    {
        Id = Guid.NewGuid(),
        HouseId = Guid.NewGuid(),
        Type = "B",
        StartDate = new DateTime(2025, 6, 5),
        EndDate = new DateTime(2025, 6, 10),
        DisplayText = "Family",
        Notes = "Quiet stay",
        CreatedByName = "John Doe",
        CreatedBy = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task HandleAsync_ShouldReturnFullResponse_WhenCallerIsAdmin()
    {
        var booking = MakeBooking();
        _repoMock.Setup(r => r.GetByMonthAsync(2025, 6, default)).ReturnsAsync([booking]);

        var result = await _handler.HandleAsync(new GetBookingsQuery(2025, 6, "Admin"), default);

        Assert.True(result.IsSuccess);
        var item = result.Value.Single();
        Assert.Equal(booking.Notes, item.Notes);
        Assert.Equal(booking.CreatedByName, item.CreatedByName);
        Assert.NotNull(item.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeNotesAndCreatedByName_WhenCallerIsUser()
    {
        var booking = MakeBooking();
        _repoMock.Setup(r => r.GetByMonthAsync(2025, 6, default)).ReturnsAsync([booking]);

        var result = await _handler.HandleAsync(new GetBookingsQuery(2025, 6, "User"), default);

        Assert.True(result.IsSuccess);
        var item = result.Value.Single();
        Assert.Equal(booking.Notes, item.Notes);
        Assert.Equal(booking.CreatedByName, item.CreatedByName);
        Assert.Null(item.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterSensitiveFields_WhenCallerIsGuest()
    {
        var booking = MakeBooking();
        _repoMock.Setup(r => r.GetByMonthAsync(2025, 6, default)).ReturnsAsync([booking]);

        var result = await _handler.HandleAsync(new GetBookingsQuery(2025, 6, null), default);

        Assert.True(result.IsSuccess);
        var item = result.Value.Single();
        Assert.Null(item.Notes);
        Assert.Null(item.CreatedByName);
        Assert.Null(item.CreatedAt);
        Assert.Equal(booking.DisplayText, item.DisplayText);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoBookingsExist()
    {
        _repoMock.Setup(r => r.GetByMonthAsync(2025, 6, default)).ReturnsAsync([]);

        var result = await _handler.HandleAsync(new GetBookingsQuery(2025, 6, null), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
