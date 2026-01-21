namespace AureliLeads.Api.DTOs;

public sealed class CreateAutomationEventRequest
{
    public Guid LeadId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime? ScheduledAt { get; set; }
}
