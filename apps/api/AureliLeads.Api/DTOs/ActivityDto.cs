using System.Text.Json;

namespace AureliLeads.Api.DTOs;

public sealed class ActivityDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public JsonElement? Data { get; set; }
    public DateTime CreatedAt { get; set; }
}
