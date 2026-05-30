using Bebrakumpis.Application.Features.Houses.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Houses.Validators;

public class CreateHouseCommandValidator : AbstractValidator<CreateHouseCommand>
{
    public CreateHouseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BookingColor).NotEmpty().Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("BookingColor must be a valid hex colour (e.g. #3b82f6).");
    }
}
