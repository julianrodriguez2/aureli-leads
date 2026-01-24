using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;

namespace AureliLeads.Api.Services;

public sealed class AutomationService : IAutomationService
{
    private const int MaxAttempts = 5;
    private readonly AureliLeadsDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AutomationService> _logger;

    public AutomationService(
        AureliLeadsDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<AutomationService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

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

    public async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        // TODO: replace polling dispatch with a durable queue.
        var now = DateTime.UtcNow;

        var pendingEvents = await _dbContext.AutomationEvents
            .Where(automationEvent =>
                (automationEvent.Status == "Pending" || automationEvent.Status == "queued") &&
                automationEvent.Attempts < MaxAttempts)
            .OrderBy(automationEvent => automationEvent.ScheduledAt)
            .ThenBy(automationEvent => automationEvent.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (pendingEvents.Count == 0)
        {
            return;
        }

        var httpClient = _httpClientFactory.CreateClient();

        foreach (var automationEvent in pendingEvents)
        {
            if (automationEvent.ScheduledAt > now)
            {
                continue;
            }

            if (automationEvent.LastAttemptAt.HasValue)
            {
                var delaySeconds = Math.Min(60d, 5d * Math.Pow(2, Math.Max(0, automationEvent.Attempts - 1)));
                if (automationEvent.LastAttemptAt.Value.AddSeconds(delaySeconds) > now)
                {
                    continue;
                }
            }

            var attemptAt = DateTime.UtcNow;
            automationEvent.Attempts += 1;
            automationEvent.LastAttemptAt = attemptAt;

            if (string.IsNullOrWhiteSpace(automationEvent.TargetUrl))
            {
                automationEvent.Status = "Failed";
                automationEvent.LastError = "Missing target URL.";
                automationEvent.ProcessedAt = attemptAt;
                continue;
            }

            try
            {
                using var content = new StringContent(automationEvent.Payload ?? "{}", Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(automationEvent.TargetUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    automationEvent.Status = "Sent";
                    automationEvent.LastError = null;
                    automationEvent.ProcessedAt = DateTime.UtcNow;
                }
                else
                {
                    automationEvent.Status = automationEvent.Attempts >= MaxAttempts ? "Failed" : "Pending";
                    automationEvent.LastError = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                    if (automationEvent.Status == "Failed")
                    {
                        automationEvent.ProcessedAt = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                automationEvent.Status = automationEvent.Attempts >= MaxAttempts ? "Failed" : "Pending";
                automationEvent.LastError = ex.Message;
                if (automationEvent.Status == "Failed")
                {
                    automationEvent.ProcessedAt = DateTime.UtcNow;
                }
                _logger.LogWarning(ex, "Automation event dispatch failed for {AutomationEventId}", automationEvent.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
