namespace AureliLeads.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "aureli-leads";
    public string Audience { get; set; } = "aureli-leads";
    public string Key { get; set; } = "CHANGE_ME";
    public int ExpiryMinutes { get; set; } = 60;
    public string CookieName { get; set; } = "access_token";
}
