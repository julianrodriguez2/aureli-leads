namespace AureliLeads.Api.DTOs;

public sealed class LeadActivityDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
