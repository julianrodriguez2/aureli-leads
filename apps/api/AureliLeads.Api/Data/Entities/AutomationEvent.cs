namespace AureliLeads.Api.Data.Entities;

public sealed class AutomationEvent
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string? TargetUrl { get; set; }
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
