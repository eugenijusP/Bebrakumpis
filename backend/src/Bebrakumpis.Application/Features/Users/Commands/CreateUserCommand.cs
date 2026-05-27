using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Users.DTOs;
using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Users.Commands;

public record CreateUserCommand(string FirstName, string LastName, string Username, string Password, string Role)
    : IRequest<Result<UserResponse>>;

public class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IValidator<CreateUserCommand> validator)
    : IRequestHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<UserResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        if (await userRepository.ExistsAsync(command.Username, ct))
            return Result<UserResponse>.Conflict($"Username '{command.Username}' is already taken.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = command.FirstName,
            LastName = command.LastName,
            Username = command.Username,
            PasswordHash = passwordHasher.Hash(command.Password),
            Role = command.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(user, ct);

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
