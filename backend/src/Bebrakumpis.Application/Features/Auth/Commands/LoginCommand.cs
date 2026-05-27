using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Auth.DTOs;
using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Auth.Commands;

public record LoginCommand(string Username, string Password) : IRequest<Result<string>>;

public class LoginCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IValidator<LoginCommand> validator) : IRequestHandler<LoginCommand, Result<string>>
{
    public async Task<Result<string>> HandleAsync(LoginCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<string>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await userRepository.GetByUsernameAsync(command.Username, ct);
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result<string>.Unauthorized("Invalid username or password.");

        if (!user.IsActive)
            return Result<string>.Unauthorized("This account has been deactivated.");

        return Result<string>.Success(tokenService.GenerateToken(user));
    }
}
