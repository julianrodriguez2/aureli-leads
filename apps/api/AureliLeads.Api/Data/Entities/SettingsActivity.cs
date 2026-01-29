namespace AureliLeads.Api.Data.Entities;

public sealed class SettingsActivity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
