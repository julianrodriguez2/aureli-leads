namespace AureliLeads.Api.DTOs;

public sealed class UpdateWebhookSettingsRequest
{
    public string WebhookTargetUrl { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
    public bool? RotateSecret { get; set; }
}
