using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.DTOs;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Houses.Commands;

public record UpdateHouseCommand(Guid Id, string Name, string BookingColor, string? Description, string? PhotoUrl, List<string> Amenities)
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

        if (!string.Equals(house.Name, command.Name, StringComparison.OrdinalIgnoreCase)
            && await houseRepository.ExistsForOtherAsync(command.Name, command.Id, ct))
            return Result<HouseResponse>.Conflict($"A house named '{command.Name}' already exists.");

        house.Name = command.Name;
        house.BookingColor = command.BookingColor;
        house.Description = command.Description;
        house.PhotoUrl = command.PhotoUrl;
        house.Amenities = command.Amenities;

        await houseRepository.UpdateAsync(house, ct);

        return Result<HouseResponse>.Success(new HouseResponse
        {
            Id = house.Id,
            Name = house.Name,
            BookingColor = house.BookingColor,
            Description = house.Description,
            PhotoUrl = house.PhotoUrl,
            Amenities = house.Amenities,
            CreatedAt = house.CreatedAt
        });
    }
}
