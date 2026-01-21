using AureliLeads.Api.DTOs;

namespace AureliLeads.Api.Services;

public sealed class AutomationService : IAutomationService
{
    public Task<IReadOnlyList<AutomationEventListDto>> GetEventsAsync(CancellationToken cancellationToken)
    {
        // TODO: implement automation event list retrieval.
        throw new NotImplementedException("TODO");
    }

    public Task<AutomationEventDetailDto?> GetEventAsync(Guid id, CancellationToken cancellationToken)
    {
        // TODO: implement automation event detail retrieval.
        throw new NotImplementedException("TODO");
    }

    public Task<AutomationEventDetailDto> EnqueueAsync(CreateAutomationEventRequest request, CancellationToken cancellationToken)
    {
        // TODO: implement automation event enqueue.
        throw new NotImplementedException("TODO");
    }

    public Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        // TODO: implement dispatch processing.
        throw new NotImplementedException("TODO");
    }
}
