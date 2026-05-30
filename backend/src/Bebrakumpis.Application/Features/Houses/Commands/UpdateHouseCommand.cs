using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.DTOs;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Houses.Commands;

public record UpdateHouseCommand(Guid Id, string Name, string BookingColor)
    : IRequest<Result<HouseResponse>>;

public class UpdateHouseCommandHandler(
    IHouseRepository houseRepository,
    IValidator<UpdateHouseCommand> validator)
    : IRequestHandler<UpdateHouseCommand, Result<HouseResponse>>
{
    public async Task<Result<HouseResponse>> HandleAsync(UpdateHouseCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<HouseResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var house = await houseRepository.GetByIdAsync(command.Id, ct);
        if (house is null)
            return Result<HouseResponse>.NotFound($"House '{command.Id}' not found.");

        house.Name = command.Name;
        house.BookingColor = command.BookingColor;

        await houseRepository.UpdateAsync(house, ct);

        return Result<HouseResponse>.Success(new HouseResponse
        {
            Id = house.Id,
            Name = house.Name,
            BookingColor = house.BookingColor,
            CreatedAt = house.CreatedAt
        });
    }
}
