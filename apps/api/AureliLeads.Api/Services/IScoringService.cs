namespace AureliLeads.Api.Services;

public interface IScoringService
{
    Task<int> CalculateScoreAsync(Guid leadId, CancellationToken cancellationToken);
}
