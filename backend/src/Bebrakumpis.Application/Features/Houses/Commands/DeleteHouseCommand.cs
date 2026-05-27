using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Domain.Interfaces;

namespace Bebrakumpis.Application.Features.Houses.Commands;

public record DeleteHouseCommand(Guid Id) : IRequest<Result>;

public class DeleteHouseCommandHandler(IHouseRepository houseRepository)
    : IRequestHandler<DeleteHouseCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteHouseCommand command, CancellationToken ct)
    {
        var house = await houseRepository.GetByIdAsync(command.Id, ct);
        if (house is null)
            return Result.NotFound($"House '{command.Id}' not found.");

        if (await houseRepository.HasBookingsAsync(command.Id, ct))
            return Result.Conflict("Cannot delete a house that has existing bookings.");

        await houseRepository.DeleteAsync(command.Id, ct);
        return Result.Success();
    }
}
