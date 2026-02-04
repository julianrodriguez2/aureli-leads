namespace AureliLeads.Api.Domain;

public static class AutomationEventStatuses
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Queued = "queued";

    private static readonly string[] AllStatuses = { Pending, Sent, Failed };

    public static IReadOnlyList<string> All => AllStatuses;

    public static string? Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (status.Equals(Queued, StringComparison.OrdinalIgnoreCase))
        {
            return Pending;
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
