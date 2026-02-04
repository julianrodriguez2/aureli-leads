using AureliLeads.Api.Auth;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.Domain;
using AureliLeads.Api.DTOs;
using AureliLeads.Api.Infrastructure;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/automation-events")]
public sealed class AutomationEventsController : ControllerBase
{
    private const int MaxAttempts = 10;
    private readonly AureliLeadsDbContext _dbContext;
    private readonly IAutomationService _automationService;
    private readonly ILogger<AutomationEventsController> _logger;

    public AutomationEventsController(
        AureliLeadsDbContext dbContext,
        IAutomationService automationService,
        ILogger<AutomationEventsController> logger)
    {
        _dbContext = dbContext;
        _automationService = automationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AutomationEventDto>>> GetEvents(
        [FromQuery] string? status,
        [FromQuery] string? eventType,
        [FromQuery] Guid? leadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "createdAt_desc",
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = Math.Min(pageSize, 100);

        var query = _dbContext.AutomationEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = AutomationEventStatuses.Normalize(status);
            if (normalizedStatus is null)
            {
                return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Invalid status value."));
            }

            if (normalizedStatus == AutomationEventStatuses.Pending)
            {
                query = query.Where(evt => evt.Status == AutomationEventStatuses.Pending || evt.Status == AutomationEventStatuses.Queued);
            }
            else
            {
                query = query.Where(evt => evt.Status == normalizedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            if (!AutomationEventTypes.IsValid(eventType))
            {
                return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Invalid eventType value."));
            }

            var trimmedType = eventType.Trim();
            query = query.Where(evt => evt.EventType == trimmedType);
        }

        if (leadId.HasValue && leadId.Value != Guid.Empty)
        {
            query = query.Where(evt => evt.LeadId == leadId.Value);
        }

        query = sort switch
        {
            "createdAt_asc" => query.OrderBy(evt => evt.CreatedAt),
            _ => query.OrderByDescending(evt => evt.CreatedAt)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Min(page, totalPages);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(evt => new AutomationEventDto
            {
                Id = evt.Id,
                LeadId = evt.LeadId,
                EventType = evt.EventType,
                Status = evt.Status,
                AttemptCount = evt.Attempts,
                LastAttemptAt = evt.LastAttemptAt,
                LastError = evt.LastError,
                CreatedAt = evt.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<AutomationEventDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }

    [EnableRateLimiting("retry")]
    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> RetryEvent(Guid id, CancellationToken cancellationToken)
    {
        if (!Roles.IsAdmin(User))
        {
            return Forbid();
        }

        var automationEvent = await _dbContext.AutomationEvents
            .FirstOrDefaultAsync(evt => evt.Id == id, cancellationToken);

        if (automationEvent is null)
        {
            return NotFound();
        }

        if (string.Equals(automationEvent.Status, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(ApiErrorFactory.Create(HttpContext, "conflict", "Already sent."));
        }

        if (automationEvent.Attempts >= MaxAttempts)
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Max attempts reached."));
        }

        var isRetryable = string.Equals(automationEvent.Status, "Failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(automationEvent.Status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(automationEvent.Status, "queued", StringComparison.OrdinalIgnoreCase);

        if (!isRetryable)
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Event is not retryable."));
        }

        automationEvent.Status = "Pending";
        automationEvent.LastError = null;
        automationEvent.LastAttemptAt = null;

        var now = DateTime.UtcNow;
        _dbContext.LeadActivities.Add(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = automationEvent.LeadId,
            Type = "WebhookRetryQueued",
            Notes = "Retry queued",
            DataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                automationEventId = automationEvent.Id,
                attemptCount = automationEvent.Attempts
            }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Automation event retry queued {AutomationEventId}", automationEvent.Id);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public ActionResult<AutomationEventDetailDto> GetEvent(Guid id)
    {
        // TODO: implement automation event detail retrieval.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost]
    public ActionResult<AutomationEventDetailDto> CreateEvent([FromBody] CreateAutomationEventRequest request)
    {
        // TODO: implement automation event creation.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost("{id:guid}/dispatch")]
    public IActionResult DispatchEvent(Guid id)
    {
        // TODO: implement immediate dispatch.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

}
