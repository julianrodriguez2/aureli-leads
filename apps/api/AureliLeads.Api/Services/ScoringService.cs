namespace AureliLeads.Api.Services;

public sealed class ScoringService : IScoringService
{
    public Task<int> CalculateScoreAsync(Guid leadId, CancellationToken cancellationToken)
    {
        // TODO: implement scoring logic.
        throw new NotImplementedException("TODO");
    }
}
