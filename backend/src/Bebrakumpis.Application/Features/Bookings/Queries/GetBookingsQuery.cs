using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Common.Result;
using Bebrakumpis.Application.Features.Bookings.DTOs;
using Bebrakumpis.Domain.Interfaces;

namespace Bebrakumpis.Application.Features.Bookings.Queries;

public record GetBookingsQuery(int Year, int Month, string? CallerRole) : IRequest<Result<IEnumerable<BookingResponse>>>;

public class GetBookingsQueryHandler(IBookingRepository bookingRepository)
    : IRequestHandler<GetBookingsQuery, Result<IEnumerable<BookingResponse>>>
{
    public async Task<Result<IEnumerable<BookingResponse>>> HandleAsync(GetBookingsQuery query, CancellationToken ct)
    {
        var bookings = await bookingRepository.GetByMonthAsync(query.Year, query.Month, ct);

        var response = bookings.Select(b => new BookingResponse
        {
            Id = b.Id,
            HouseId = b.HouseId,
            Type = b.Type,
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            DisplayText = b.DisplayText,
            Notes = query.CallerRole is not null ? b.Notes : null,
            CreatedByName = query.CallerRole is not null && !string.IsNullOrWhiteSpace(b.CreatedByName)
                ? b.CreatedByName!.Trim() : null,
            CreatedAt = query.CallerRole == "Admin" ? b.CreatedAt : null
        });

        return Result<IEnumerable<BookingResponse>>.Success(response);
    }
}
