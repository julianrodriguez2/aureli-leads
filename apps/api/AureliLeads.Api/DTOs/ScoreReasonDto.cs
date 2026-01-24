namespace AureliLeads.Api.DTOs;

public sealed class ScoreReasonDto
{
    public string Rule { get; set; } = string.Empty;
    public int Delta { get; set; }
}
