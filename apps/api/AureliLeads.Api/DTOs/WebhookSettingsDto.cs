namespace AureliLeads.Api.DTOs;

public sealed class WebhookSettingsDto
{
    public string? WebhookTargetUrl { get; set; }
    public string? WebhookSecret { get; set; }
    public bool HasWebhookSecret { get; set; }
}
