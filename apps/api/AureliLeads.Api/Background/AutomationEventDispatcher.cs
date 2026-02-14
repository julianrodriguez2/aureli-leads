using AureliLeads.Api.Services;

namespace AureliLeads.Api.Background;

public sealed class AutomationEventDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationEventDispatcher> _logger;

    public AutomationEventDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationEventDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationEventDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Resolve scoped services inside each polling cycle.
                using var scope = _scopeFactory.CreateScope();
                var automationService = scope.ServiceProvider.GetRequiredService<IAutomationService>();
                await automationService.DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Automation dispatch cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
