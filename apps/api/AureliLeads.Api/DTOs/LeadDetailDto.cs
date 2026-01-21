namespace AureliLeads.Api.DTOs;

public sealed class LeadDetailDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LeadActivityDto> Activities { get; set; } = new();
    public List<AutomationEventDetailDto> AutomationEvents { get; set; } = new();
}
