using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Users.Commands;

public record ChangePasswordCommand(Guid TargetId, Guid RequesterId, string? CurrentPassword, string NewPassword)
    : IRequest<Result>;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IValidator<ChangePasswordCommand> validator)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await userRepository.GetByIdAsync(command.TargetId, ct);
        if (user is null)
            return Result.NotFound($"User '{command.TargetId}' not found.");

        if (command.RequesterId == command.TargetId)
        {
            if (string.IsNullOrEmpty(command.CurrentPassword) || !passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
                return Result.ValidationFailure("Current password is incorrect.");
        }

        var newHash = passwordHasher.Hash(command.NewPassword);
        await userRepository.ChangePasswordAsync(command.TargetId, newHash, ct);
        return Result.Success();
    }
}
