using Bebrakumpis.Application.Features.Bookings.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Bookings.Validators;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.HouseId).NotEmpty();
        RuleFor(x => x.Type).Must(t => t == "B" || t == "R")
            .WithMessage("Type must be 'B' (Booked) or 'R' (Reserved).");
        RuleFor(x => x.DisplayText).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
