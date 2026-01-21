namespace AureliLeads.Api.DTOs;

public sealed class AuthResponseDto
{
    public UserDto User { get; set; } = new();
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInMinutes { get; set; }
}
