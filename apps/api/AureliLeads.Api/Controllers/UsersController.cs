using AureliLeads.Api.Auth;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly AureliLeadsDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(AureliLeadsDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        if (!Roles.IsAdmin(User))
        {
            return Forbid();
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var results = users.Select(user => new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = Roles.Normalize(user.Role),
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        }).ToList();

        return Ok(results);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!Roles.IsAdmin(User))
        {
            return Forbid();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest();
        }

        if (!IsValidEmail(request.Email))
        {
            return BadRequest(new { message = "Invalid email" });
        }

        if (request.Password.Trim().Length < 8)
        {
            return BadRequest(new { message = "Password too short" });
        }

        if (!Roles.IsValidRole(request.Role))
        {
            return BadRequest(new { message = "Invalid role" });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => EF.Functions.ILike(user.Email, email), cancellationToken);

        if (existing)
        {
            return Conflict(new { message = "Email already exists" });
        }

        var now = DateTime.UtcNow;
        var normalizedRole = Roles.Normalize(request.Role);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Role = normalizedRole,
            IsActive = true,
            CreatedAt = now
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        _dbContext.SettingsActivities.Add(new SettingsActivity
        {
            Id = Guid.NewGuid(),
            Type = "UserCreated",
            DataJson = JsonSerializer.Serialize(new
            {
                actorEmail = GetUserEmail(User),
                targetUserEmail = user.Email,
                role = normalizedRole
            }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = normalizedRole,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<ActionResult<UserDto>> UpdateUserRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!Roles.IsAdmin(User))
        {
            return Forbid();
        }

        if (request is null || !Roles.IsValidRole(request.Role))
        {
            return BadRequest(new { message = "Invalid role" });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var oldRole = Roles.Normalize(user.Role);
        var newRole = Roles.Normalize(request.Role);
        user.Role = newRole;

        _dbContext.SettingsActivities.Add(new SettingsActivity
        {
            Id = Guid.NewGuid(),
            Type = "UserRoleChanged",
            DataJson = JsonSerializer.Serialize(new
            {
                actorEmail = GetUserEmail(User),
                targetUserEmail = user.Email,
                oldRole,
                newRole
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

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

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!Roles.IsAdmin(User))
        {
            return Forbid();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest();
        }

        if (request.Password.Trim().Length < 8)
        {
            return BadRequest(new { message = "Password too short" });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.SettingsActivities.Add(new SettingsActivity
        {
            Id = Guid.NewGuid(),
            Type = "UserPasswordReset",
            DataJson = JsonSerializer.Serialize(new
            {
                actorEmail = GetUserEmail(User),
                targetUserEmail = user.Email
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Email);
    }
}
