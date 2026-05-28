using Bebrakumpis.API.Common;
using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Features.Bookings.Commands;
using Bebrakumpis.Application.Features.Bookings.DTOs;
using Bebrakumpis.Application.Features.Bookings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bebrakumpis.API.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public class BookingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByMonth([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12)
            return ValidationProblem("year must be 1–9999 and month must be 1–12.");

        var callerRole = User.Identity?.IsAuthenticated == true
            ? (User.IsInRole("Admin") ? "Admin" : "User")
            : null;

        var result = await mediator.SendAsync(new GetBookingsQuery(year, month, callerRole), ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var requesterId = User.GetCurrentUserId();
        if (requesterId is null) return Unauthorized();

        var result = await mediator.SendAsync(
            new CreateBookingCommand(requesterId.Value, request.HouseId, request.Type,
                request.StartDate, request.EndDate, request.DisplayText, request.Notes), ct);
        if (!result.IsSuccess)
            return result.ToProblemResult(this);

        return Created(string.Empty, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookingRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(
            new UpdateBookingCommand(id, request.HouseId, request.Type,
                request.StartDate, request.EndDate, request.DisplayText, request.Notes), ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new DeleteBookingCommand(id), ct);
        return result.ToActionResult(this, NoContent);
    }
}
