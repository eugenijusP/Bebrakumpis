using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Users.Commands;
using Bebrakumpis.Application.Features.Users.Validators;
using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using Moq;

namespace Bebrakumpis.Tests.Application.Users;

public class ChangePasswordCommandTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandTests()
    {
        _handler = new ChangePasswordCommandHandler(_repoMock.Object, _hasherMock.Object, new ChangePasswordCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_ShouldChangePassword_WhenOwnAccountAndCurrentPasswordCorrect()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, PasswordHash = "hash", Username = "john" };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("OldPass1", "hash")).Returns(true);
        _hasherMock.Setup(h => h.Hash("NewPass1")).Returns("newhash");

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(id, id, "OldPass1", "NewPass1"), default);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.ChangePasswordAsync(id, "newhash", default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenCurrentPasswordIsWrong()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, PasswordHash = "hash", Username = "john" };
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("WrongPass", "hash")).Returns(false);

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(id, id, "WrongPass", "NewPass1"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldChangePassword_WhenAdminChangesOtherUsersPassword()
    {
        var targetId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User { Id = targetId, PasswordHash = "hash", Username = "john" };
        _repoMock.Setup(r => r.GetByIdAsync(targetId, default)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Hash("NewPass1")).Returns("newhash");

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(targetId, adminId, null, "NewPass1"), default);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.ChangePasswordAsync(targetId, "newhash", default), Times.Once);
        _hasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(Guid.NewGuid(), Guid.NewGuid(), null, "NewPass1"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationFailure_WhenNewPasswordIsTooShort()
    {
        var result = await _handler.HandleAsync(
            new ChangePasswordCommand(Guid.NewGuid(), Guid.NewGuid(), null, "ab"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ValidationFailure, result.ErrorType);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
