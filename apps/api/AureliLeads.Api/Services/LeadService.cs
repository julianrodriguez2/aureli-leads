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

    public Task<IReadOnlyList<LeadListDto>> GetLeadsAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Leads
            .AsNoTracking()
            .OrderByDescending(lead => lead.CreatedAt)
            .Select(lead => new LeadListDto
            {
                Id = lead.Id,
                FirstName = lead.FirstName,
                LastName = lead.LastName,
                Email = lead.Email,
                Company = lead.Company,
                Status = lead.Status,
                Score = lead.Score,
                CreatedAt = lead.CreatedAt
            })
            .ToListAsync(cancellationToken);
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
