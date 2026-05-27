using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Users.DTOs;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Users.Commands;

public record UpdateUserCommand(Guid Id, Guid RequesterId, string FirstName, string LastName, string Role, bool IsActive)
    : IRequest<Result<UserResponse>>;

public class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IValidator<UpdateUserCommand> validator)
    : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<UserResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await userRepository.GetByIdAsync(command.Id, ct);
        if (user is null)
            return Result<UserResponse>.NotFound($"User '{command.Id}' not found.");

        if (command.RequesterId == command.Id && !command.IsActive)
            return Result<UserResponse>.Conflict("You cannot deactivate your own account.");

        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.Role = command.Role;
        user.IsActive = command.IsActive;

        await userRepository.UpdateAsync(user, ct);

        return Result<UserResponse>.Success(new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }
}
