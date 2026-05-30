using Bebrakumpis.Application.Features.Houses.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Houses.Validators;

public class UpdateHouseCommandValidator : AbstractValidator<UpdateHouseCommand>
{
    public UpdateHouseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BookingColor).NotEmpty().Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("BookingColor must be a valid hex colour (e.g. #3b82f6).");
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.PhotoUrl)
            .MaximumLength(2048)
            .Matches(@"^https?://").WithMessage("PhotoUrl must start with http:// or https://.")
            .When(x => x.PhotoUrl is not null);
        RuleFor(x => x.Amenities).Must(a => a.Count <= 10).WithMessage("Amenities must not exceed 10 items.");
        RuleForEach(x => x.Amenities).NotEmpty().MaximumLength(100);
    }
}
