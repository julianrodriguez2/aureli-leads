using AureliLeads.Api.Auth;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.DTOs;
using AureliLeads.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
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
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        AureliLeadsDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
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
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Email and password are required."));
        }

        if (!Validation.IsValidEmail(request.Email))
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Invalid email."));
        }

        if (!Validation.IsValidPassword(request.Password, 8))
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Password too short."));
        }

        if (!Roles.IsValidRole(request.Role))
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Invalid role."));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => EF.Functions.ILike(user.Email, email), cancellationToken);

        if (existing)
        {
            return Conflict(ApiErrorFactory.Create(HttpContext, "conflict", "Email already exists."));
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
        _logger.LogInformation("User created {TargetEmail}", user.Email);

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
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Invalid role."));
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
        _logger.LogInformation("User role changed {TargetEmail} {OldRole}->{NewRole}", user.Email, oldRole, newRole);

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
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Password is required."));
        }

        if (!Validation.IsValidPassword(request.Password, 8))
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Password too short."));
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
        _logger.LogInformation("User password reset {TargetEmail}", user.Email);
        return NoContent();
    }

    private static string? GetUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Email);
    }
}
