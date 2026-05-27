using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Users.Commands;
using Bebrakumpis.Application.Features.Users.Validators;
using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Users;

public class CreateUserCommandTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandTests()
    {
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _handler = new CreateUserCommandHandler(_repoMock.Object, _hasherMock.Object, new CreateUserCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUserResponse_WhenCommandIsValid()
    {
        _repoMock.Setup(r => r.ExistsAsync("john", default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>(), default)).ReturnsAsync(Guid.NewGuid());

        var result = await _handler.HandleAsync(
            new CreateUserCommand("John", "Doe", "john", "Secret1", "User"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value.FirstName);
        Assert.Equal("john", result.Value.Username);
        Assert.Equal("User", result.Value.Role);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenUsernameAlreadyExists()
    {
        _repoMock.Setup(r => r.ExistsAsync("john", default)).ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new CreateUserCommand("John", "Doe", "john", "Secret1", "User"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenFirstNameIsEmpty()
    {
        var result = await _handler.HandleAsync(
            new CreateUserCommand("", "Doe", "john", "Secret1", "User"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenRoleIsInvalid()
    {
        var result = await _handler.HandleAsync(
            new CreateUserCommand("John", "Doe", "john", "Secret1", "SuperAdmin"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenPasswordIsTooShort()
    {
        var result = await _handler.HandleAsync(
            new CreateUserCommand("John", "Doe", "john", "ab", "User"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
    }
}
