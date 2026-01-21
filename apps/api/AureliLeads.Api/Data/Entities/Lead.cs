namespace AureliLeads.Api.Data.Entities;

public sealed class Lead
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string Status { get; set; } = "new";
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<LeadActivity> Activities { get; set; } = new List<LeadActivity>();
    public ICollection<AutomationEvent> AutomationEvents { get; set; } = new List<AutomationEvent>();
}
