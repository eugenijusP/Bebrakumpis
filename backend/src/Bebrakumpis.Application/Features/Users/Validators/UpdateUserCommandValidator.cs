using Bebrakumpis.Application.Features.Users.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Users.Validators;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.Role).NotEmpty().Must(r => r == "Admin" || r == "User")
            .WithMessage("Role must be 'Admin' or 'User'.");
    }
}
