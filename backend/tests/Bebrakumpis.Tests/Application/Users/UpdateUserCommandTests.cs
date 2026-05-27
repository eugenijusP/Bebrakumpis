using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Users.Commands;
using Bebrakumpis.Application.Features.Users.Validators;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Users;

public class UpdateUserCommandTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandTests()
    {
        _handler = new UpdateUserCommandHandler(_repoMock.Object, new UpdateUserCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUpdatedUser_WhenCommandIsValid()
    {
        var id = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var user = new User { Id = id, FirstName = "Old", LastName = "Name", Username = "john", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(user);

        var result = await _handler.HandleAsync(
            new UpdateUserCommand(id, requesterId, "New", "Name", "Admin", true), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", result.Value.FirstName);
        Assert.Equal("Admin", result.Value.Role);
        _repoMock.Verify(r => r.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(
            new UpdateUserCommand(id, requesterId, "Name", "Last", "User", true), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenAdminTriesToDeactivateThemselves()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, FirstName = "Name", LastName = "Last", Username = "self", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(user);

        var result = await _handler.HandleAsync(
            new UpdateUserCommand(id, id, "Name", "Last", "Admin", false), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenRoleIsInvalid()
    {
        var result = await _handler.HandleAsync(
            new UpdateUserCommand(Guid.NewGuid(), Guid.NewGuid(), "Name", "Last", "Ghost", true), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }
}
