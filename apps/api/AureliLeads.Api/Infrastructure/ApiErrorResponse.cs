using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AureliLeads.Api.Infrastructure;

public sealed class ApiErrorResponse
{
    public ApiErrorDetail Error { get; set; } = new();
}

public sealed class ApiErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

public static class ApiErrorFactory
{
    public static ApiErrorResponse Create(HttpContext context, string code, string message, object? details = null)
    {
        return new ApiErrorResponse
        {
            Error = new ApiErrorDetail
            {
                Code = code,
                Message = message,
                Details = details,
                TraceId = CorrelationId.Get(context)
            }
        };
    }

    public static ApiErrorResponse CreateValidationError(HttpContext context, ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return Create(context, "validation_error", "Validation failed.", errors);
    }

    public static async Task WriteAsync(HttpContext context, int statusCode, string code, string message, object? details = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var options = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
        var payload = Create(context, code, message, details);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, options.Value.JsonSerializerOptions));
    }

    public static (string Code, string Message) MapStatusCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => ("bad_request", "Bad request."),
            StatusCodes.Status401Unauthorized => ("unauthorized", "Unauthorized."),
            StatusCodes.Status403Forbidden => ("forbidden", "Forbidden."),
            StatusCodes.Status404NotFound => ("not_found", "Not found."),
            StatusCodes.Status409Conflict => ("conflict", "Conflict."),
            StatusCodes.Status429TooManyRequests => ("rate_limited", "Too many requests."),
            StatusCodes.Status500InternalServerError => ("internal_error", "An unexpected error occurred."),
            _ => ("error", "Request failed.")
        };
    }
}
