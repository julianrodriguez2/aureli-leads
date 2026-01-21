using AureliLeads.Api.DTOs;

namespace AureliLeads.Api.Services;

public interface ILeadService
{
    Task<IReadOnlyList<LeadListDto>> GetLeadsAsync(CancellationToken cancellationToken);
    Task<LeadDetailDto?> GetLeadAsync(Guid id, CancellationToken cancellationToken);
    Task<LeadDetailDto> CreateLeadAsync(CreateLeadRequest request, CancellationToken cancellationToken);
    Task<LeadDetailDto> UpdateLeadAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteLeadAsync(Guid id, CancellationToken cancellationToken);
}
