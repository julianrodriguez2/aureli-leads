using System.Text.Json;

namespace AureliLeads.Api.DTOs;

public sealed class LeadDetailDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<ScoreReasonDto> ScoreReasons { get; set; } = new();
    public string? Message { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public JsonElement? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
