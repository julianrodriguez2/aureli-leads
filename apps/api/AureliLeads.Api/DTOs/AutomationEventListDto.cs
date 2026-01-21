namespace AureliLeads.Api.DTOs;

public sealed class AutomationEventListDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
