using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AureliLeads.Api.Services;

public sealed class LeadService : ILeadService
{
    private readonly AureliLeadsDbContext _dbContext;

    public LeadService(AureliLeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<LeadListItemDto>> GetLeadsAsync(LeadListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        pageSize = Math.Min(pageSize, 100);

        var leadsQuery = _dbContext.Leads.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var search = $"%{query.Q.Trim()}%";
            leadsQuery = leadsQuery.Where(lead =>
                EF.Functions.ILike(lead.FirstName, search) ||
                EF.Functions.ILike(lead.LastName, search) ||
                EF.Functions.ILike(lead.Email, search) ||
                (lead.Phone != null && EF.Functions.ILike(lead.Phone, search)) ||
                (lead.Message != null && EF.Functions.ILike(lead.Message, search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            leadsQuery = leadsQuery.Where(lead => lead.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = query.Source.Trim();
            leadsQuery = leadsQuery.Where(lead => lead.Source == source);
        }

        if (query.MinScore.HasValue)
        {
            var minScore = Math.Max(0, query.MinScore.Value);
            leadsQuery = leadsQuery.Where(lead => lead.Score >= minScore);
        }

        leadsQuery = query.Sort switch
        {
            "createdAt_asc" => leadsQuery.OrderBy(lead => lead.CreatedAt),
            "score_desc" => leadsQuery.OrderByDescending(lead => lead.Score),
            "score_asc" => leadsQuery.OrderBy(lead => lead.Score),
            _ => leadsQuery.OrderByDescending(lead => lead.CreatedAt)
        };

        var totalItems = await leadsQuery.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Min(page, totalPages);

        var items = await leadsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lead => new LeadListItemDto
            {
                Id = lead.Id,
                FirstName = lead.FirstName,
                LastName = lead.LastName,
                Email = lead.Email,
                Phone = lead.Phone,
                Source = lead.Source,
                Status = lead.Status,
                Score = lead.Score,
                CreatedAt = lead.CreatedAt,
                LastActivityAt = lead.Activities
                    .OrderByDescending(activity => activity.CreatedAt)
                    .Select(activity => (DateTime?)activity.CreatedAt)
                    .FirstOrDefault() ?? lead.CreatedAt,
                AutomationStatus = lead.AutomationEvents
                    .OrderByDescending(evt => evt.CreatedAt)
                    .Select(evt => evt.Status)
                    .FirstOrDefault() ?? "None"
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<LeadListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public Task<LeadDetailDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken)
    {
        // TODO: implement lead detail retrieval.
        throw new NotImplementedException("TODO");
    }

    public Task<LeadDetailDto> CreateLeadAsync(CreateLeadRequest request, CancellationToken cancellationToken)
    {
        // TODO: implement lead creation.
        throw new NotImplementedException("TODO");
    }

    public Task<LeadDetailDto> UpdateLeadAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken)
    {
        // TODO: implement lead update.
        throw new NotImplementedException("TODO");
    }

    public Task<bool> DeleteLeadAsync(Guid id, CancellationToken cancellationToken)
    {
        // TODO: implement lead deletion.
        throw new NotImplementedException("TODO");
    }
}
