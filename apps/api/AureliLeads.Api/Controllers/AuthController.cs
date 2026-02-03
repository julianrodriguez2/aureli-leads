using AureliLeads.Api.Auth;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AureliLeads.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AureliLeadsDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        AureliLeadsDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        IPasswordHasher<User> passwordHasher,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
        _passwordHasher = passwordHasher;
        _environment = environment;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var passwordCheck = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordCheck == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        if (passwordCheck == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = CreateToken(user);
        Response.Cookies.Append(_jwtOptions.CookieName, token, BuildCookieOptions());

        var normalizedRole = Roles.Normalize(user.Role);

        return Ok(new AuthResponseDto
        {
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = normalizedRole,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            },
            TokenType = "Bearer",
            ExpiresInMinutes = _jwtOptions.ExpiryMinutes
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // TODO: invalidate server-side sessions if applicable.
        Response.Cookies.Delete(_jwtOptions.CookieName, new CookieOptions { Path = "/" });
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = Roles.Normalize(user.Role),
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        });
    }

    private string CreateToken(User user)
    {
        var normalizedRole = Roles.Normalize(user.Role);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, normalizedRole)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private CookieOptions BuildCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_environment.IsDevelopment(),
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)
        };
    }
}
