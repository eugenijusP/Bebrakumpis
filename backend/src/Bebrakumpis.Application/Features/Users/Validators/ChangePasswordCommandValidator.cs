using Bebrakumpis.Application.Features.Users.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Users.Validators;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(200);
    }
}
