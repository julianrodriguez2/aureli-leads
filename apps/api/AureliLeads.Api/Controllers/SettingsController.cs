using AureliLeads.Api.Auth;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.DTOs;
using AureliLeads.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly AureliLeadsDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        AureliLeadsDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment,
        ILogger<SettingsController> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<WebhookSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == "WebhookTargetUrl" || setting.Key == "WebhookSecret")
            .ToListAsync(cancellationToken);

        var targetUrl = settings.FirstOrDefault(setting => setting.Key == "WebhookTargetUrl")?.Value;
        var secret = settings.FirstOrDefault(setting => setting.Key == "WebhookSecret")?.Value;

        return Ok(new WebhookSettingsDto
        {
            WebhookTargetUrl = targetUrl,
            WebhookSecret = null,
            HasWebhookSecret = !string.IsNullOrWhiteSpace(secret)
        });
    }

    [HttpPatch("webhook")]
    public async Task<ActionResult<WebhookSettingsDto>> UpdateWebhookSettings(
        [FromBody] UpdateWebhookSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!Roles.IsAdmin(User))
        {
            return Forbid();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.WebhookTargetUrl))
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "WebhookTargetUrl is required."));
        }

        var targetUrl = request.WebhookTargetUrl.Trim();
        if (!Validation.IsValidWebhookUrl(targetUrl))
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Invalid webhookTargetUrl."));
        }

        var now = DateTime.UtcNow;
        var existingSettings = await _dbContext.Settings
            .Where(setting => setting.Key == "WebhookTargetUrl" || setting.Key == "WebhookSecret")
            .ToListAsync(cancellationToken);

        var existingUrl = existingSettings.FirstOrDefault(setting => setting.Key == "WebhookTargetUrl")?.Value;
        var existingSecret = existingSettings.FirstOrDefault(setting => setting.Key == "WebhookSecret")?.Value;

        var secretChanged = false;
        string? nextSecret = null;

        if (request.RotateSecret is true)
        {
            nextSecret = GenerateSecret();
            secretChanged = true;
        }
        else if (request.WebhookSecret is not null)
        {
            var trimmedSecret = request.WebhookSecret.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSecret))
            {
                if (trimmedSecret.Length < 8 || trimmedSecret.Length > 200)
                {
                    return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "Invalid webhookSecret length."));
                }

                nextSecret = trimmedSecret;
                secretChanged = true;
            }
        }

        UpsertSetting(existingSettings, "WebhookTargetUrl", targetUrl, now);

        if (secretChanged)
        {
            UpsertSetting(existingSettings, "WebhookSecret", nextSecret ?? string.Empty, now);
        }

        _dbContext.SettingsActivities.Add(new SettingsActivity
        {
            Id = Guid.NewGuid(),
            Type = "WebhookSettingsUpdated",
            DataJson = JsonSerializer.Serialize(new
            {
                oldUrl = existingUrl,
                newUrl = targetUrl,
                secretChanged,
                actorEmail = GetUserEmail(User)
            }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Webhook settings updated by {ActorEmail}. Secret changed: {SecretChanged}", GetUserEmail(User), secretChanged);

        return Ok(new WebhookSettingsDto
        {
            WebhookTargetUrl = targetUrl,
            WebhookSecret = null,
            HasWebhookSecret = secretChanged ? !string.IsNullOrWhiteSpace(nextSecret) : !string.IsNullOrWhiteSpace(existingSecret)
        });
    }

    [EnableRateLimiting("webhook-test")]
    [HttpPost("webhook/test")]
    public async Task<ActionResult<object>> TestWebhook(CancellationToken cancellationToken)
    {
        if (!Roles.IsAdmin(User))
        {
            return Forbid();
        }

        var settings = await _dbContext.Settings
            .AsNoTracking()
            .Where(setting => setting.Key == "WebhookTargetUrl" || setting.Key == "WebhookSecret")
            .ToListAsync(cancellationToken);

        var targetUrl = settings.FirstOrDefault(setting => setting.Key == "WebhookTargetUrl")?.Value;
        var secret = settings.FirstOrDefault(setting => setting.Key == "WebhookSecret")?.Value;

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return BadRequest(ApiErrorFactory.Create(HttpContext, "validation_error", "WebhookTargetUrl not configured."));
        }

        var payload = new
        {
            eventType = "TestWebhook",
            timestamp = DateTime.UtcNow,
            message = "Hello from Aureli Leads",
            environment = _environment.EnvironmentName
        };

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Aureli-Event", "TestWebhook");

            if (!string.IsNullOrWhiteSpace(secret))
            {
                request.Headers.Add("X-Aureli-Secret", secret);
            }

            var response = await httpClient.SendAsync(request, cancellationToken);
            var ok = response.IsSuccessStatusCode;

            _logger.LogInformation("Webhook test sent. Status {StatusCode}", (int)response.StatusCode);

            return Ok(new
            {
                ok,
                statusCode = (int)response.StatusCode,
                error = ok ? null : response.ReasonPhrase
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook test failed.");
            return Ok(new
            {
                ok = false,
                statusCode = 0,
                error = ex.Message
            });
        }
    }

    private static string? GetUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Email);
    }

    private static string GenerateSecret()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }

    private void UpsertSetting(List<Setting> existingSettings, string key, string value, DateTime now)
    {
        var setting = existingSettings.FirstOrDefault(candidate => candidate.Key == key);
        if (setting is null)
        {
            setting = new Setting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                UpdatedAt = now
            };

            _dbContext.Settings.Add(setting);
            existingSettings.Add(setting);
            return;
        }

        setting.Value = value;
        setting.UpdatedAt = now;
    }
}
