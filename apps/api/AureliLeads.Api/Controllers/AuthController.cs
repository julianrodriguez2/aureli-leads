using AureliLeads.Api.Auth;
using AureliLeads.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AureliLeads.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;

    public AuthController(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("login")]
    public ActionResult<AuthResponseDto> Login([FromBody] LoginRequestDto request)
    {
        // TODO: validate credentials, issue JWT, set httpOnly cookie.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // TODO: invalidate server-side sessions if applicable.
        Response.Cookies.Delete(_jwtOptions.CookieName);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserDto> Me()
    {
        // TODO: return current user.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
