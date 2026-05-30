using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.Queries;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Houses;

public class GetAllHousesQueryTests
{
    private readonly Mock<IHouseRepository> _repoMock = new();
    private readonly GetAllHousesQueryHandler _handler;

    public GetAllHousesQueryTests()
    {
        _handler = new GetAllHousesQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllHouses_WhenHousesExist()
    {
        var houses = new List<House>
        {
            new() { Id = Guid.NewGuid(), Name = "Namas 1", BookingColor = "#3b82f6", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Namas 2", BookingColor = "#10b981", CreatedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(houses);

        var result = await _handler.HandleAsync(new GetAllHousesQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoHousesExist()
    {
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var result = await _handler.HandleAsync(new GetAllHousesQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
