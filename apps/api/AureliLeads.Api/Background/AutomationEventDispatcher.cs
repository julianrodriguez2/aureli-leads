using AureliLeads.Api.Services;

namespace AureliLeads.Api.Background;

public sealed class AutomationEventDispatcher : BackgroundService
{
    private readonly IAutomationService _automationService;
    private readonly ILogger<AutomationEventDispatcher> _logger;

    public AutomationEventDispatcher(IAutomationService automationService, ILogger<AutomationEventDispatcher> logger)
    {
        _automationService = automationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationEventDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: tune dispatch cadence and error handling.
            await _automationService.DispatchPendingAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
