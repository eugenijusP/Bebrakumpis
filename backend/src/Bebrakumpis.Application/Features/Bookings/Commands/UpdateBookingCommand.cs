using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Bookings.DTOs;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Bookings.Commands;

public record UpdateBookingCommand(
    Guid Id,
    Guid HouseId,
    string Type,
    DateTime StartDate,
    DateTime EndDate,
    string DisplayText,
    string? Notes)
    : IRequest<Result<BookingResponse>>;

public class UpdateBookingCommandHandler(
    IBookingRepository bookingRepository,
    IHouseRepository houseRepository,
    IValidator<UpdateBookingCommand> validator)
    : IRequestHandler<UpdateBookingCommand, Result<BookingResponse>>
{
    public async Task<Result<BookingResponse>> HandleAsync(UpdateBookingCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<BookingResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var booking = await bookingRepository.GetByIdAsync(command.Id, ct);
        if (booking is null)
            return Result<BookingResponse>.NotFound($"Booking '{command.Id}' not found.");

        var house = await houseRepository.GetByIdAsync(command.HouseId, ct);
        if (house is null)
            return Result<BookingResponse>.NotFound($"House '{command.HouseId}' not found.");

        booking.HouseId = command.HouseId;
        booking.Type = command.Type;
        booking.StartDate = command.StartDate.Date;
        booking.EndDate = command.EndDate.Date;
        booking.DisplayText = command.DisplayText;
        booking.Notes = command.Notes;

        await bookingRepository.UpdateAsync(booking, ct);

        return Result<BookingResponse>.Success(new BookingResponse
        {
            Id = booking.Id,
            HouseId = booking.HouseId,
            Type = booking.Type,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            DisplayText = booking.DisplayText,
            Notes = booking.Notes,
            CreatedAt = booking.CreatedAt
        });
    }
}
