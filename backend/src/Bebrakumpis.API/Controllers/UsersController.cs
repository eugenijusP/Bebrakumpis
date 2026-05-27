using Bebrakumpis.API.Common;
using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Features.Users.Commands;
using Bebrakumpis.Application.Features.Users.DTOs;
using Bebrakumpis.Application.Features.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bebrakumpis.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.SendAsync(new GetAllUsersQuery(), ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(
            new CreateUserCommand(request.FirstName, request.LastName, request.Username, request.Password, request.Role), ct);
        if (!result.IsSuccess)
            return result.ToProblemResult(this);

        return CreatedAtAction(nameof(GetAll), new { }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var requesterId = User.GetCurrentUserId();
        if (requesterId is null) return Unauthorized();

        var result = await mediator.SendAsync(
            new UpdateUserCommand(id, requesterId.Value, request.FirstName, request.LastName, request.Role, request.IsActive), ct);
        return result.ToActionResult(this, Ok);
    }

    [HttpPut("{id:guid}/password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var requesterId = User.GetCurrentUserId();
        if (requesterId is null) return Unauthorized();

        if (requesterId.Value != id && !User.IsInRole("Admin"))
            return Forbid();

        var result = await mediator.SendAsync(
            new ChangePasswordCommand(id, requesterId.Value, request.CurrentPassword, request.NewPassword), ct);
        return result.ToActionResult(this, NoContent);
    }
}
