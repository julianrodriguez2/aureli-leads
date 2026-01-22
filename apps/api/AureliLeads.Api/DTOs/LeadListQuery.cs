namespace AureliLeads.Api.DTOs;

public sealed class LeadListQuery
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
    public int? MinScore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string Sort { get; set; } = "createdAt_desc";
}
