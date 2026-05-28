using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Domain.Interfaces;

namespace Bebrakumpis.Application.Features.Bookings.Commands;

public record DeleteBookingCommand(Guid Id) : IRequest<Result>;

public class DeleteBookingCommandHandler(IBookingRepository bookingRepository)
    : IRequestHandler<DeleteBookingCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteBookingCommand command, CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(command.Id, ct);
        if (booking is null)
            return Result.NotFound($"Booking '{command.Id}' not found.");

        await bookingRepository.DeleteAsync(command.Id, ct);
        return Result.Success();
    }
}
