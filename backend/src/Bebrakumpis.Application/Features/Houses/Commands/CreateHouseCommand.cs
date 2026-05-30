using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Houses.DTOs;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Houses.Commands;

public record CreateHouseCommand(string Name, string BookingColor)
    : IRequest<Result<HouseResponse>>;

public class CreateHouseCommandHandler(
    IHouseRepository houseRepository,
    IValidator<CreateHouseCommand> validator)
    : IRequestHandler<CreateHouseCommand, Result<HouseResponse>>
{
    public async Task<Result<HouseResponse>> HandleAsync(CreateHouseCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<HouseResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        if (await houseRepository.ExistsAsync(command.Name, ct))
            return Result<HouseResponse>.Conflict($"A house named '{command.Name}' already exists.");

        var house = new House
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            BookingColor = command.BookingColor,
            CreatedAt = DateTime.UtcNow
        };

        await houseRepository.CreateAsync(house, ct);

        return Result<HouseResponse>.Success(new HouseResponse
        {
            Id = house.Id,
            Name = house.Name,
            BookingColor = house.BookingColor,
            CreatedAt = house.CreatedAt
        });
    }
}
