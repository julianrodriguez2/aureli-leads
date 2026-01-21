using AureliLeads.Api.DTOs;

namespace AureliLeads.Api.Services;

public interface IAutomationService
{
    Task<IReadOnlyList<AutomationEventListDto>> GetEventsAsync(CancellationToken cancellationToken);
    Task<AutomationEventDetailDto?> GetEventAsync(Guid id, CancellationToken cancellationToken);
    Task<AutomationEventDetailDto> EnqueueAsync(CreateAutomationEventRequest request, CancellationToken cancellationToken);
    Task DispatchPendingAsync(CancellationToken cancellationToken);
}
