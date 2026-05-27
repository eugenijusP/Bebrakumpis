using Bebrakumpis.Application.Features.Users.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Users.Validators;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty().Must(r => r == "Admin" || r == "User")
            .WithMessage("Role must be 'Admin' or 'User'.");
    }
}
