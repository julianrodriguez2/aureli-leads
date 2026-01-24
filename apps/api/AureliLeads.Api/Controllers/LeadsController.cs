using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.DTOs;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/leads")]
public sealed class LeadsController : ControllerBase
{
    private readonly AureliLeadsDbContext _dbContext;
    private readonly ILeadService _leadService;
    private static readonly string[] AllowedStatuses = { "New", "Contacted", "Qualified", "Disqualified" };

    public LeadsController(AureliLeadsDbContext dbContext, ILeadService leadService)
    {
        _dbContext = dbContext;
        _leadService = leadService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<LeadListItemDto>>> GetLeads(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] string? source,
        [FromQuery] int? minScore,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "createdAt_desc",
        CancellationToken cancellationToken = default)
    {
        var query = new LeadListQuery
        {
            Q = q,
            Status = status,
            Source = source,
            MinScore = minScore,
            Page = page,
            PageSize = pageSize,
            Sort = sort ?? "createdAt_desc"
        };

        var leads = await _leadService.GetLeadsAsync(query, cancellationToken);
        return Ok(leads);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadDetailDto>> GetLead(Guid id, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (lead is null)
        {
            return NotFound();
        }

        return Ok(MapLeadDetail(lead));
    }

    [HttpGet("{id:guid}/activities")]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetLeadActivities(Guid id, CancellationToken cancellationToken)
    {
        var leadExists = await _dbContext.Leads
            .AsNoTracking()
            .AnyAsync(lead => lead.Id == id, cancellationToken);

        if (!leadExists)
        {
            return NotFound();
        }

        var activities = await _dbContext.LeadActivities
            .AsNoTracking()
            .Where(activity => activity.LeadId == id)
            .OrderByDescending(activity => activity.CreatedAt)
            .ToListAsync(cancellationToken);

        var results = activities.Select(activity => new ActivityDto
        {
            Id = activity.Id,
            Type = activity.Type,
            Data = ParseJsonElement(activity.DataJson),
            CreatedAt = activity.CreatedAt
        }).ToList();

        return Ok(results);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<LeadDetailDto>> UpdateLeadStatus(
        Guid id,
        [FromBody] UpdateLeadStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanUpdateStatus(User))
        {
            return Forbid();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest();
        }

        var normalizedStatus = NormalizeStatus(request.Status);
        if (normalizedStatus is null)
        {
            return BadRequest();
        }

        var lead = await _dbContext.Leads
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (lead is null)
        {
            return NotFound();
        }

        if (string.Equals(lead.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(MapLeadDetail(lead));
        }

        var now = DateTime.UtcNow;
        var previousStatus = lead.Status;
        lead.Status = normalizedStatus;
        lead.UpdatedAt = now;

        var activities = new List<LeadActivity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "StatusChanged",
                Notes = "Status updated",
                DataJson = JsonSerializer.Serialize(new { from = previousStatus, to = normalizedStatus }),
                CreatedAt = now
            }
        };

        var targetUrl = await _dbContext.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == "WebhookTargetUrl")
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(targetUrl))
        {
            var payload = JsonSerializer.Serialize(new
            {
                eventType = "StatusChanged",
                leadId = lead.Id,
                oldStatus = previousStatus,
                newStatus = normalizedStatus,
                timestamp = now,
                lead = new
                {
                    id = lead.Id,
                    firstName = lead.FirstName,
                    lastName = lead.LastName,
                    email = lead.Email,
                    phone = lead.Phone,
                    status = normalizedStatus,
                    source = lead.Source,
                    score = lead.Score
                }
            });

            _dbContext.AutomationEvents.Add(new AutomationEvent
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                EventType = "StatusChanged",
                Payload = payload,
                TargetUrl = targetUrl,
                Status = "Pending",
                ScheduledAt = now,
                CreatedAt = now
            });
        }
        else
        {
            activities.Add(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "WebhookSkipped",
                Notes = "Webhook target missing",
                DataJson = JsonSerializer.Serialize(new { reason = "Missing WebhookTargetUrl setting." }),
                CreatedAt = now
            });
        }

        _dbContext.LeadActivities.AddRange(activities);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapLeadDetail(lead));
    }

    // TODO: add rescore endpoint.

    [HttpPost]
    public ActionResult<LeadDetailDto> CreateLead([FromBody] CreateLeadRequest request)
    {
        // TODO: implement lead creation.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    private static T? TryDeserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static LeadDetailDto MapLeadDetail(Lead lead)
    {
        var tags = TryDeserialize<string[]>(lead.TagsJson) ?? Array.Empty<string>();
        var scoreReasons = TryDeserialize<List<ScoreReasonDto>>(lead.ScoreReasonsJson) ?? new List<ScoreReasonDto>();

        return new LeadDetailDto
        {
            Id = lead.Id,
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            Phone = lead.Phone,
            Source = lead.Source,
            Status = lead.Status,
            Score = lead.Score,
            ScoreReasons = scoreReasons,
            Message = lead.Message,
            Tags = tags,
            Metadata = ParseJsonElement(lead.MetadataJson),
            CreatedAt = lead.CreatedAt,
            UpdatedAt = lead.UpdatedAt
        };
    }

    private static bool CanUpdateStatus(ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return role.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || role.Equals("agent", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeStatus(string status)
    {
        foreach (var allowed in AllowedStatuses)
        {
            if (string.Equals(allowed, status, StringComparison.OrdinalIgnoreCase))
            {
                return allowed;
            }
        }

        return null;
    }

    private static JsonElement? ParseJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [HttpPut("{id:guid}")]
    public ActionResult<LeadDetailDto> UpdateLead(Guid id, [FromBody] UpdateLeadRequest request)
    {
        // TODO: implement lead update.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteLead(Guid id)
    {
        // TODO: implement lead deletion.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
