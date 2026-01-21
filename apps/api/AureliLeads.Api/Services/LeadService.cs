using AureliLeads.Api.DTOs;

namespace AureliLeads.Api.Services;

public sealed class LeadService : ILeadService
{
    public Task<IReadOnlyList<LeadListDto>> GetLeadsAsync(CancellationToken cancellationToken)
    {
        // TODO: implement lead list retrieval.
        throw new NotImplementedException("TODO");
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
