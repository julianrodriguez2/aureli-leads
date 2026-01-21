namespace AureliLeads.Api.DTOs;

public sealed class UpdateLeadRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Status { get; set; }
}
