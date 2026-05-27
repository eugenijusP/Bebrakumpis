using Bebrakumpis.Application.Features.Users.Queries;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Users;

public class GetAllUsersQueryTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly GetAllUsersQueryHandler _handler;

    public GetAllUsersQueryTests()
    {
        _handler = new GetAllUsersQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllUsers()
    {
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<User>
        {
            new() { Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Smith", Username = "alice", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), FirstName = "Bob",   LastName = "Jones", Username = "bob",   Role = "User",  IsActive = true, CreatedAt = DateTime.UtcNow }
        });

        var result = await _handler.HandleAsync(new GetAllUsersQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<User>());

        var result = await _handler.HandleAsync(new GetAllUsersQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
