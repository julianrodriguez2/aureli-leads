using AureliLeads.Api.Auth;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public SettingsController(
        AureliLeadsDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
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
            return BadRequest();
        }

        var targetUrl = request.WebhookTargetUrl.Trim();
        if (!IsValidWebhookUrl(targetUrl))
        {
            return BadRequest(new { message = "Invalid webhookTargetUrl" });
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
                    return BadRequest(new { message = "Invalid webhookSecret length" });
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

        return Ok(new WebhookSettingsDto
        {
            WebhookTargetUrl = targetUrl,
            WebhookSecret = null,
            HasWebhookSecret = secretChanged ? !string.IsNullOrWhiteSpace(nextSecret) : !string.IsNullOrWhiteSpace(existingSecret)
        });
    }

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
            return BadRequest(new { message = "WebhookTargetUrl not configured" });
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

            return Ok(new
            {
                ok,
                statusCode = (int)response.StatusCode,
                error = ok ? null : response.ReasonPhrase
            });
        }
        catch (Exception ex)
        {
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

    private static bool IsValidWebhookUrl(string url)
    {
        if (url.Length > 500)
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
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
