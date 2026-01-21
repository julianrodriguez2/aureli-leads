namespace AureliLeads.Api.DTOs;

public sealed class AutomationEventDetailDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
