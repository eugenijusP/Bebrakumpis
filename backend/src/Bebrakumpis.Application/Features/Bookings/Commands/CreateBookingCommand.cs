using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Bookings.DTOs;
using Bebrakumpis.Domain.Entities;
using Bebrakumpis.Domain.Interfaces;
using FluentValidation;

namespace Bebrakumpis.Application.Features.Bookings.Commands;

public record CreateBookingCommand(
    Guid RequesterId,
    Guid HouseId,
    string Type,
    DateTime StartDate,
    DateTime EndDate,
    string DisplayText,
    string? Notes)
    : IRequest<Result<BookingResponse>>;

public class CreateBookingCommandHandler(
    IBookingRepository bookingRepository,
    IHouseRepository houseRepository,
    IValidator<CreateBookingCommand> validator)
    : IRequestHandler<CreateBookingCommand, Result<BookingResponse>>
{
    public async Task<Result<BookingResponse>> HandleAsync(CreateBookingCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<BookingResponse>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));

        var house = await houseRepository.GetByIdAsync(command.HouseId, ct);
        if (house is null)
            return Result<BookingResponse>.NotFound($"House '{command.HouseId}' not found.");

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            HouseId = command.HouseId,
            Type = command.Type,
            StartDate = command.StartDate.Date,
            EndDate = command.EndDate.Date,
            DisplayText = command.DisplayText,
            Notes = command.Notes,
            CreatedBy = command.RequesterId,
            CreatedAt = DateTime.UtcNow
        };

        await bookingRepository.CreateAsync(booking, ct);

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
