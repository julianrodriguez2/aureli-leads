namespace AureliLeads.Api.DTOs;

public sealed class AutomationEventDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
}
