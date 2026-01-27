using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.DTOs;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/automation-events")]
public sealed class AutomationEventsController : ControllerBase
{
    private readonly AureliLeadsDbContext _dbContext;
    private readonly IAutomationService _automationService;

    public AutomationEventsController(AureliLeadsDbContext dbContext, IAutomationService automationService)
    {
        _dbContext = dbContext;
        _automationService = automationService;
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
            var trimmedStatus = status.Trim();
            query = query.Where(evt => evt.Status == trimmedStatus);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
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
