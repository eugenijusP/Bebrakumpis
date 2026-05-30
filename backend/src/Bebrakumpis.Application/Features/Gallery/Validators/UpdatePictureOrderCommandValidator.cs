using Bebrakumpis.Application.Features.Gallery.Commands;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Gallery.Validators;

public class UpdatePictureOrderCommandValidator : AbstractValidator<UpdatePictureOrderCommand>
{
    public UpdatePictureOrderCommandValidator()
    {
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater.");
    }
}
