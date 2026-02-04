namespace AureliLeads.Api.Infrastructure;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public static string Get(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var value) && value is string correlationId)
        {
            return correlationId;
        }

        return context.TraceIdentifier;
    }
}
