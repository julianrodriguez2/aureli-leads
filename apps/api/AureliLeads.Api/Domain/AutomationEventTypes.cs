namespace AureliLeads.Api.Domain;

public static class AutomationEventTypes
{
    public const string LeadCreated = "LeadCreated";
    public const string LeadScored = "LeadScored";
    public const string StatusChanged = "StatusChanged";

    private static readonly string[] AllTypes = { LeadCreated, LeadScored, StatusChanged };

    public static IReadOnlyList<string> All => AllTypes;

    public static bool IsValid(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return false;
        }

        return AllTypes.Any(type => type.Equals(eventType, StringComparison.OrdinalIgnoreCase));
    }
}
