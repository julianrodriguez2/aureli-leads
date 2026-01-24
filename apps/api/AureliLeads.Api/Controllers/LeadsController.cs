using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.DTOs;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/leads")]
public sealed class LeadsController : ControllerBase
{
    private readonly AureliLeadsDbContext _dbContext;
    private readonly ILeadService _leadService;

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

        var tags = TryDeserialize<string[]>(lead.TagsJson) ?? Array.Empty<string>();
        var scoreReasons = TryDeserialize<List<ScoreReasonDto>>(lead.ScoreReasonsJson) ?? new List<ScoreReasonDto>();

        return Ok(new LeadDetailDto
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
        });
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

    // TODO: add status update endpoint.
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
