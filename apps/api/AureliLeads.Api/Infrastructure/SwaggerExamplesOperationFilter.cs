using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AureliLeads.Api.Infrastructure;

public sealed class SwaggerExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? string.Empty;
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();

        if (method == "PATCH" && path.Contains("api/leads/{id}/status", StringComparison.OrdinalIgnoreCase))
        {
            SetRequestExample(operation, new OpenApiObject
            {
                ["status"] = new OpenApiString("Qualified")
            });
        }

        if (method == "POST" && path.Contains("api/leads/{id}/notes", StringComparison.OrdinalIgnoreCase))
        {
            SetRequestExample(operation, new OpenApiObject
            {
                ["text"] = new OpenApiString("Left a voicemail and sent a follow-up email.")
            });
        }

        if (method == "POST" && path.Contains("api/leads/{id}/score", StringComparison.OrdinalIgnoreCase))
        {
            operation.Description = string.IsNullOrWhiteSpace(operation.Description)
                ? "No request body required."
                : $"{operation.Description}\n\nNo request body required.";
        }

        if (method == "PATCH" && path.Contains("api/settings/webhook", StringComparison.OrdinalIgnoreCase))
        {
            SetRequestExample(operation, new OpenApiObject
            {
                ["webhookTargetUrl"] = new OpenApiString("https://your-n8n-instance/webhook/aureli"),
                ["webhookSecret"] = new OpenApiString("replace-with-secure-secret")
            });
        }
    }

    private static void SetRequestExample(OpenApiOperation operation, IOpenApiAny example)
    {
        if (operation.RequestBody?.Content is null)
        {
            return;
        }

        if (operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType.Example = example;
        }
    }
}
