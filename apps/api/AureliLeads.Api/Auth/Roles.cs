using System.Security.Claims;

namespace AureliLeads.Api.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Agent = "Agent";
    public const string ReadOnly = "ReadOnly";

    public static string Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return ReadOnly;
        }

        var trimmed = role.Trim();
        if (trimmed.Equals(Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Admin;
        }

        if (trimmed.Equals(Agent, StringComparison.OrdinalIgnoreCase))
        {
            return Agent;
        }

        if (trimmed.Equals(ReadOnly, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("read-only", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("read_only", StringComparison.OrdinalIgnoreCase))
        {
            return ReadOnly;
        }

        return trimmed;
    }

    public static bool IsValidRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return role.Equals(Admin, StringComparison.OrdinalIgnoreCase)
            || role.Equals(Agent, StringComparison.OrdinalIgnoreCase)
            || role.Equals(ReadOnly, StringComparison.OrdinalIgnoreCase)
            || role.Equals("read-only", StringComparison.OrdinalIgnoreCase)
            || role.Equals("read_only", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAdmin(ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);
        return role is not null && role.Equals(Admin, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAdminOrAgent(ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return role.Equals(Admin, StringComparison.OrdinalIgnoreCase)
            || role.Equals(Agent, StringComparison.OrdinalIgnoreCase);
    }
}
