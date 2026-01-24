namespace AureliLeads.Api.Data.Entities;

public sealed class LeadActivity
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? DataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
