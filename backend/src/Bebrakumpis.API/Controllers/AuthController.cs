using Bebrakumpis.API.Common;
using Bebrakumpis.Application.Common.CQRS;
using Bebrakumpis.Application.Features.Auth.Commands;
using Bebrakumpis.Application.Features.Auth.DTOs;
using Bebrakumpis.Application.Features.Auth.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bebrakumpis.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new LoginCommand(request.Username, request.Password), ct);
        if (!result.IsSuccess)
            return result.ToProblemResult(this);

        Response.Cookies.Append("bh_auth", result.Value, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsEnvironment("Testing"),
            SameSite = env.IsEnvironment("Testing") ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        return Ok(new { message = "Login successful." });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("bh_auth");
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await mediator.SendAsync(new GetMeQuery(User), ct);
        return result.ToActionResult(this, Ok);
    }
}
