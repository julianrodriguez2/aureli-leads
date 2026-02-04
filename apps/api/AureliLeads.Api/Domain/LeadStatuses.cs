namespace AureliLeads.Api.Domain;

public static class LeadStatuses
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Disqualified = "Disqualified";

    private static readonly string[] AllStatuses = { New, Contacted, Qualified, Disqualified };

    public static IReadOnlyList<string> All => AllStatuses;

    public static string? Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        foreach (var allowed in AllStatuses)
        {
            if (string.Equals(allowed, status, StringComparison.OrdinalIgnoreCase))
            {
                return allowed;
            }
        }

        return null;
    }
}
