using AureliLeads.Api.Auth;
using AureliLeads.Api.Background;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.Infrastructure;
using AureliLeads.Api.Middleware;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text.Json;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "NextJsDev";
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:3000" };

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errorResponse = ApiErrorFactory.CreateValidationError(context.HttpContext, context.ModelState);
            return new BadRequestObjectResult(errorResponse);
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aureli Leads API",
        Version = "v1",
        Description = "Authentication uses JWT stored in the httpOnly `access_token` cookie. " +
                      "Call /api/auth/login to set the cookie and include it on subsequent requests."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        },
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Description = "JWT Authorization header using the Bearer scheme."
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });

    options.OperationFilter<AureliLeads.Api.Infrastructure.SwaggerExamplesOperationFilter>();
});

builder.Services.AddDbContext<AureliLeadsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddHttpClient(string.Empty, client => client.Timeout = TimeSpan.FromSeconds(10));

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHostedService<AutomationEventDispatcher>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        var (code, message) = ApiErrorFactory.MapStatusCode(StatusCodes.Status429TooManyRequests);
        await ApiErrorFactory.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, code, message);
    };

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("webhook-test", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("retry", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(jwtOptions.CookieName, out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("UnhandledException");
            logger.LogError(feature.Error, "Unhandled exception.");
        }

        await ApiErrorFactory.WriteAsync(context, StatusCodes.Status500InternalServerError, "internal_error",
            "An unexpected error occurred.");
    });
});

app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.HasStarted)
    {
        return;
    }

    if (response.ContentLength is > 0 || !string.IsNullOrEmpty(response.ContentType))
    {
        return;
    }

    var (code, message) = ApiErrorFactory.MapStatusCode(response.StatusCode);
    await ApiErrorFactory.WriteAsync(statusContext.HttpContext, response.StatusCode, code, message);
});

await ApplyMigrationsAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    await SeedDemoUsersAsync(app.Services);
    await SeedSampleLeadsAsync(app.Services);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static async Task ApplyMigrationsAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AureliLeadsDbContext>();
    var hasMigrations = dbContext.Database.GetMigrations().Any();
    if (hasMigrations)
    {
        await dbContext.Database.MigrateAsync();
        return;
    }

    // No EF migrations were added yet. EnsureCreated handles schema bootstrap for dev/demo mode.
    if (!await TableExistsAsync(dbContext, "Users"))
    {
        await dbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "__EFMigrationsHistory";""");
        await dbContext.Database.EnsureCreatedAsync();
    }
}

static async Task<bool> TableExistsAsync(AureliLeadsDbContext dbContext, string tableName)
{
    var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name = @tableName
        );
        """;
    command.Parameters.AddWithValue("tableName", tableName);
    var result = await command.ExecuteScalarAsync();
    return result is bool exists && exists;
}

static async Task SeedDemoUsersAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AureliLeadsDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var now = DateTime.UtcNow;

    var demoUsers = new[]
    {
        new { Email = "admin@local.test", Password = "Admin123!", Role = Roles.Admin },
        new { Email = "agent@local.test", Password = "Agent123!", Role = Roles.Agent },
        new { Email = "readonly@local.test", Password = "Readonly123!", Role = Roles.ReadOnly }
    };

    var existingEmails = await dbContext.Users
        .AsNoTracking()
        .Select(user => user.Email)
        .ToListAsync();

    var normalizedExisting = new HashSet<string>(existingEmails.Select(email => email.ToLowerInvariant()));
    var addedAny = false;

    foreach (var demoUser in demoUsers)
    {
        var email = demoUser.Email.Trim().ToLowerInvariant();
        if (normalizedExisting.Contains(email))
        {
            continue;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Role = demoUser.Role,
            IsActive = true,
            CreatedAt = now
        };

        user.PasswordHash = hasher.HashPassword(user, demoUser.Password);
        dbContext.Users.Add(user);
        addedAny = true;
    }

    if (addedAny)
    {
        await dbContext.SaveChangesAsync();
    }
}

static async Task SeedSampleLeadsAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AureliLeadsDbContext>();
    var random = new Random(42);
    var sources = new[] { "web", "google_ads", "referral", "n8n" };
    var statuses = new[] { "New", "Contacted", "Qualified", "Disqualified" };
    var localCityNames = new[] { "Riverside", "Corona", "Eastvale", "Norco" };
    var localCitiesLower = localCityNames.Select(city => city.ToLowerInvariant()).ToArray();
    var landingPages = new[] { "/pricing", "/demo", "/contact", "/solutions/real-estate", "/solutions/operations" };
    var utmCampaigns = new[] { "spring-launch", "q1-growth", "partner-referral", "retargeting", "webinar-jan" };
    var highIntentKeywords = new[] { "quote", "pricing", "price", "estimate", "book", "appointment", "schedule", "asap" };
    var spamKeywords = new[] { "backlinks", "seo services", "guest post", "rank your site", "casino" };
    var inquiryMessages = new[]
    {
        "Interested in a demo next week for our operations team.",
        "Looking to compare pricing tiers before rollout.",
        "Asked about onboarding timeline and integrations.",
        "Needs approval from leadership; follow up Friday.",
        "We want to automate follow-ups for inbound leads."
    };
    var highIntentMessages = new[]
    {
        "Requesting a pricing quote and estimate.",
        "Can we schedule an appointment ASAP?",
        "Looking to book a demo and discuss pricing.",
        "Interested in a quick estimate for our team."
    };
    var spamMessages = new[]
    {
        "We offer backlinks and SEO services.",
        "Guest post opportunities to rank your site.",
        "Casino backlinks campaign inquiry."
    };
    var noteTemplates = new[]
    {
        "Left voicemail, follow up tomorrow.",
        "Asked for a case study in their industry.",
        "Needs approval from finance team.",
        "Requested a custom workflow walkthrough."
    };
    var companies = new[]
    {
        "Summit Realty", "Nimbus Logistics", "Brightline Dental", "Lumen Fitness",
        "Evergreen Interiors", "Catalyst Insurance", "Northwind Labs", "Monarch Builders",
        "Atlas Advisors", "Harborview Solar", "Bluecrest HVAC", "Sierra Legal"
    };
    var firstNames = new[] { "Ava", "Mateo", "Priya", "Liam", "Noah", "Sophia", "Maya", "Ethan", "Zoe", "Lucas", "Mila", "Aria" };
    var lastNames = new[] { "Chen", "Silva", "Nair", "Johnson", "Garcia", "Patel", "Kim", "Singh", "Brown", "Nguyen", "Rodriguez", "Lopez" };
    var tagSets = new[]
    {
        new[] { "inbound", "priority" },
        new[] { "enterprise" },
        new[] { "trial", "smb" },
        new[] { "partner" },
        new[] { "repeat", "high-value" }
    };
    const string demoWebhookUrl = "https://hooks.example.com/aureli";

    bool ContainsKeyword(string? value, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var lower = value.ToLowerInvariant();
        foreach (var keyword in keywords)
        {
            if (lower.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    Dictionary<string, object?> ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>();
        }
    }

    (int Score, List<object> Reasons) BuildScore(Lead lead, string? metadataJson)
    {
        var score = 0;
        var reasons = new List<object>();

        void AddReason(string rule, int delta)
        {
            reasons.Add(new { rule, delta });
            score += delta;
        }

        if (!string.IsNullOrWhiteSpace(lead.Email))
        {
            AddReason("HasEmail", 20);
        }

        if (!string.IsNullOrWhiteSpace(lead.Phone))
        {
            AddReason("HasPhone", 20);
        }

        if (ContainsKeyword(lead.Message, highIntentKeywords))
        {
            AddReason("HighIntentKeywords", 15);
        }

        if (string.Equals(lead.Source, "google_ads", StringComparison.OrdinalIgnoreCase))
        {
            AddReason("SourceWeightGoogleAds", 10);
        }
        else if (string.Equals(lead.Source, "referral", StringComparison.OrdinalIgnoreCase))
        {
            AddReason("SourceWeightReferral", 8);
        }

        if (ContainsKeyword(metadataJson, localCitiesLower))
        {
            AddReason("LocalAreaMatch", 10);
        }

        if (ContainsKeyword(lead.Message, spamKeywords))
        {
            AddReason("SpamPenalty", -30);
        }

        if (string.IsNullOrWhiteSpace(lead.Email) && string.IsNullOrWhiteSpace(lead.Phone))
        {
            AddReason("MissingContactPenalty", -15);
        }

        score = Math.Clamp(score, 0, 100);
        return (score, reasons);
    }

    AutomationEvent BuildAutomationEvent(
        Lead lead,
        string eventType,
        string status,
        int attempts,
        string? lastError,
        DateTime createdAt)
    {
        DateTime? lastAttemptAt = status == "Pending" ? null : createdAt.AddMinutes(5);
        DateTime? processedAt = status is "Sent" or "Failed" ? createdAt.AddMinutes(5) : null;
        var payload = JsonSerializer.Serialize(new
        {
            eventType,
            leadId = lead.Id,
            timestamp = createdAt,
            lead = new
            {
                id = lead.Id,
                firstName = lead.FirstName,
                lastName = lead.LastName,
                email = lead.Email,
                status = lead.Status,
                source = lead.Source,
                score = lead.Score
            }
        });

        return new AutomationEvent
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            EventType = eventType,
            Status = status,
            Attempts = attempts,
            LastError = lastError,
            LastAttemptAt = lastAttemptAt,
            ProcessedAt = processedAt,
            ScheduledAt = createdAt.AddMinutes(1),
            CreatedAt = createdAt,
            TargetUrl = demoWebhookUrl,
            Payload = payload
        };
    }

    var existingLeads = await dbContext.Leads.AsNoTracking().ToListAsync();
    if (existingLeads.Count > 0)
    {
        var seededLeadIds = existingLeads
            .Where(lead => lead.Email.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase))
            .Select(lead => lead.Id)
            .ToList();

        if (seededLeadIds.Count == 0)
        {
            return;
        }

        var existingActivities = await dbContext.LeadActivities
            .AsNoTracking()
            .Where(activity => seededLeadIds.Contains(activity.LeadId))
            .ToListAsync();

        var existingActivityTypes = existingActivities
            .GroupBy(activity => activity.LeadId)
            .ToDictionary(
                group => group.Key,
                group => new HashSet<string>(group.Select(activity => activity.Type)));

        var existingAutomationEvents = await dbContext.AutomationEvents
            .AsNoTracking()
            .Where(automationEvent => seededLeadIds.Contains(automationEvent.LeadId))
            .ToListAsync();

        var existingEventLeadIds = new HashSet<Guid>(existingAutomationEvents.Select(evt => evt.LeadId));

        var seededLeads = await dbContext.Leads
            .Where(lead => seededLeadIds.Contains(lead.Id))
            .ToListAsync();

        var newActivities = new List<LeadActivity>();
        var newAutomationEvents = new List<AutomationEvent>();
        var hasLeadUpdates = false;

        for (var i = 0; i < seededLeads.Count; i++)
        {
            var lead = seededLeads[i];
            var leadUpdated = false;
            var activityTypes = existingActivityTypes.TryGetValue(lead.Id, out var types)
                ? types
                : new HashSet<string>();

            if (string.IsNullOrWhiteSpace(lead.TagsJson))
            {
                lead.TagsJson = JsonSerializer.Serialize(tagSets[random.Next(tagSets.Length)]);
                leadUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(lead.MetadataJson))
            {
                var metadata = new Dictionary<string, object?>
                {
                    ["utm_source"] = lead.Source,
                    ["utm_campaign"] = utmCampaigns[random.Next(utmCampaigns.Length)],
                    ["landingPage"] = landingPages[random.Next(landingPages.Length)]
                };

                if (i % 3 == 0)
                {
                    metadata["city"] = localCityNames[random.Next(localCityNames.Length)];
                }

                lead.MetadataJson = JsonSerializer.Serialize(metadata);
                leadUpdated = true;
            }
            else
            {
                var metadata = ParseMetadata(lead.MetadataJson);
                var updated = false;

                if (!metadata.ContainsKey("utm_campaign"))
                {
                    metadata["utm_campaign"] = utmCampaigns[random.Next(utmCampaigns.Length)];
                    updated = true;
                }

                if (!metadata.ContainsKey("landingPage"))
                {
                    metadata["landingPage"] = landingPages[random.Next(landingPages.Length)];
                    updated = true;
                }

                if (!ContainsKeyword(lead.MetadataJson, localCitiesLower) && i % 3 == 0)
                {
                    metadata["city"] = localCityNames[random.Next(localCityNames.Length)];
                    updated = true;
                }

                if (updated)
                {
                    lead.MetadataJson = JsonSerializer.Serialize(metadata);
                    leadUpdated = true;
                }
            }

            if (string.IsNullOrWhiteSpace(lead.ScoreReasonsJson))
            {
                var (_, scoreReasons) = BuildScore(lead, lead.MetadataJson);
                lead.ScoreReasonsJson = JsonSerializer.Serialize(scoreReasons);
                leadUpdated = true;
            }

            var hasHighIntent = ContainsKeyword(lead.Message, highIntentKeywords);
            var hasSpam = ContainsKeyword(lead.Message, spamKeywords);
            if (!hasHighIntent && !hasSpam)
            {
                string? injectedMessage = null;
                if (i % 8 == 0)
                {
                    injectedMessage = spamMessages[random.Next(spamMessages.Length)];
                }
                else if (i % 3 == 0)
                {
                    injectedMessage = highIntentMessages[random.Next(highIntentMessages.Length)];
                }
                else if (string.IsNullOrWhiteSpace(lead.Message))
                {
                    injectedMessage = inquiryMessages[random.Next(inquiryMessages.Length)];
                }

                if (!string.IsNullOrWhiteSpace(injectedMessage))
                {
                    lead.Message = string.IsNullOrWhiteSpace(lead.Message)
                        ? injectedMessage
                        : $"{lead.Message} {injectedMessage}";
                    leadUpdated = true;
                }
            }

            if (!activityTypes.Contains("Created"))
            {
                newActivities.Add(new LeadActivity
                {
                    Id = Guid.NewGuid(),
                    LeadId = lead.Id,
                    Type = "Created",
                    Notes = "Seeded lead",
                    DataJson = JsonSerializer.Serialize(new { source = "seed" }),
                    CreatedAt = lead.CreatedAt
                });
            }

            if (!activityTypes.Contains("StatusChanged") && i % 2 == 0)
            {
                newActivities.Add(new LeadActivity
                {
                    Id = Guid.NewGuid(),
                    LeadId = lead.Id,
                    Type = "StatusChanged",
                    Notes = "Status updated",
                    DataJson = JsonSerializer.Serialize(new { from = "New", to = lead.Status }),
                    CreatedAt = lead.CreatedAt.AddDays(random.Next(1, 10))
                });
            }

            if (!activityTypes.Contains("NoteAdded") && i % 4 == 0)
            {
                newActivities.Add(new LeadActivity
                {
                    Id = Guid.NewGuid(),
                    LeadId = lead.Id,
                    Type = "NoteAdded",
                    Notes = "Seeded note",
                    DataJson = JsonSerializer.Serialize(new
                    {
                        text = noteTemplates[random.Next(noteTemplates.Length)],
                        authorEmail = "agent@local.test"
                    }),
                    CreatedAt = lead.CreatedAt.AddHours(random.Next(4, 48))
                });
            }

            if (!activityTypes.Contains("Scored") && i % 3 == 0)
            {
                var (score, reasons) = BuildScore(lead, lead.MetadataJson);
                var oldScore = Math.Max(0, score - random.Next(5, 25));
                newActivities.Add(new LeadActivity
                {
                    Id = Guid.NewGuid(),
                    LeadId = lead.Id,
                    Type = "Scored",
                    Notes = "Seeded scoring run",
                    DataJson = JsonSerializer.Serialize(new { oldScore, newScore = score, reasons }),
                    CreatedAt = lead.CreatedAt.AddDays(random.Next(2, 12))
                });
            }

            if (!activityTypes.Contains("WebhookSent") && i % 5 == 0)
            {
                newActivities.Add(new LeadActivity
                {
                    Id = Guid.NewGuid(),
                    LeadId = lead.Id,
                    Type = "WebhookSent",
                    Notes = "Webhook delivered",
                    DataJson = JsonSerializer.Serialize(new { endpoint = demoWebhookUrl, status = 200 }),
                    CreatedAt = lead.CreatedAt.AddDays(random.Next(1, 8))
                });
            }
            else if (!activityTypes.Contains("WebhookFailed") && i % 7 == 0)
            {
                newActivities.Add(new LeadActivity
                {
                    Id = Guid.NewGuid(),
                    LeadId = lead.Id,
                    Type = "WebhookFailed",
                    Notes = "Webhook failed",
                    DataJson = JsonSerializer.Serialize(new { endpoint = demoWebhookUrl, error = "Timeout" }),
                    CreatedAt = lead.CreatedAt.AddDays(random.Next(1, 8))
                });
            }

            if (!existingEventLeadIds.Contains(lead.Id))
            {
                var eventStatus = i % 6 == 0 ? "Failed" : i % 4 == 0 ? "Pending" : "Sent";
                var attempts = eventStatus == "Failed" ? random.Next(2, 5) : eventStatus == "Sent" ? 1 : 0;
                var lastError = eventStatus == "Failed" ? "HTTP 500 from target" : null;
                var baseEventTime = lead.CreatedAt.AddHours(random.Next(2, 36));

                newAutomationEvents.Add(BuildAutomationEvent(lead, "LeadCreated", eventStatus, attempts, lastError, baseEventTime));

                if (i % 3 == 0)
                {
                    newAutomationEvents.Add(BuildAutomationEvent(
                        lead,
                        "LeadScored",
                        eventStatus == "Sent" ? "Sent" : "Pending",
                        eventStatus == "Sent" ? 1 : attempts,
                        eventStatus == "Failed" ? lastError : null,
                        baseEventTime.AddHours(6)));
                }

                if (i % 2 == 0)
                {
                    newAutomationEvents.Add(BuildAutomationEvent(
                        lead,
                        "StatusChanged",
                        eventStatus,
                        attempts,
                        lastError,
                        baseEventTime.AddHours(3)));
                }
            }

            if (leadUpdated)
            {
                lead.UpdatedAt = DateTime.UtcNow;
                hasLeadUpdates = true;
            }
        }

        if (newActivities.Count > 0)
        {
            dbContext.LeadActivities.AddRange(newActivities);
            hasLeadUpdates = true;
        }

        if (newAutomationEvents.Count > 0)
        {
            dbContext.AutomationEvents.AddRange(newAutomationEvents);
            hasLeadUpdates = true;
        }

        if (hasLeadUpdates)
        {
            await dbContext.SaveChangesAsync();
        }

        return;
    }

    var leads = new List<Lead>();
    var activities = new List<LeadActivity>();
    var automationEvents = new List<AutomationEvent>();
    var now = DateTime.UtcNow;

    for (var i = 0; i < 36; i++)
    {
        var firstName = firstNames[i % firstNames.Length];
        var lastName = lastNames[(i + 3) % lastNames.Length];
        var company = companies[(i + 2) % companies.Length];
        var createdAt = now.AddDays(-random.Next(0, 30)).AddMinutes(-random.Next(0, 1440));
        var updatedAt = createdAt.AddHours(random.Next(1, 72));
        if (updatedAt > now)
        {
            updatedAt = now;
        }

        var source = sources[random.Next(sources.Length)];
        var status = statuses[random.Next(statuses.Length)];
        var tags = tagSets[random.Next(tagSets.Length)];
        var city = i % 2 == 0 ? localCityNames[random.Next(localCityNames.Length)] : null;
        var message = i % 7 == 0
            ? spamMessages[random.Next(spamMessages.Length)]
            : i % 3 == 0
                ? highIntentMessages[random.Next(highIntentMessages.Length)]
                : inquiryMessages[random.Next(inquiryMessages.Length)];
        var metadata = new Dictionary<string, object?>
        {
            ["utm_source"] = source,
            ["utm_campaign"] = utmCampaigns[random.Next(utmCampaigns.Length)],
            ["landingPage"] = landingPages[random.Next(landingPages.Length)]
        };

        if (!string.IsNullOrWhiteSpace(city))
        {
            metadata["city"] = city;
        }

        var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com";
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = $"+1 (951) 555-{random.Next(1000, 9999)}",
            Company = company,
            Source = source,
            Status = status,
            Message = message,
            TagsJson = JsonSerializer.Serialize(tags),
            MetadataJson = JsonSerializer.Serialize(metadata),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        var (score, reasons) = BuildScore(lead, lead.MetadataJson);
        lead.Score = score;
        lead.ScoreReasonsJson = JsonSerializer.Serialize(reasons);

        leads.Add(lead);

        activities.Add(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            Type = "Created",
            Notes = "Seeded lead",
            DataJson = JsonSerializer.Serialize(new { source = "seed", channel = source }),
            CreatedAt = createdAt
        });

        if (i % 2 == 0)
        {
            activities.Add(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "StatusChanged",
                Notes = "Seeded activity",
                DataJson = JsonSerializer.Serialize(new { from = "New", to = status }),
                CreatedAt = createdAt.AddDays(random.Next(1, 10))
            });
        }

        if (i % 4 == 0)
        {
            activities.Add(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "NoteAdded",
                Notes = "Seeded note",
                DataJson = JsonSerializer.Serialize(new
                {
                    text = noteTemplates[random.Next(noteTemplates.Length)],
                    authorEmail = "agent@local.test"
                }),
                CreatedAt = createdAt.AddHours(random.Next(6, 36))
            });
        }

        if (i % 3 == 0)
        {
            var oldScore = Math.Max(0, score - random.Next(5, 25));
            activities.Add(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "Scored",
                Notes = "Seeded scoring run",
                DataJson = JsonSerializer.Serialize(new { oldScore, newScore = score, reasons }),
                CreatedAt = createdAt.AddDays(random.Next(2, 12))
            });
        }

        if (i % 5 == 0)
        {
            activities.Add(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "WebhookSent",
                Notes = "Webhook delivered",
                DataJson = JsonSerializer.Serialize(new { endpoint = demoWebhookUrl, status = 200 }),
                CreatedAt = createdAt.AddDays(random.Next(1, 10))
            });
        }
        else if (i % 6 == 0)
        {
            activities.Add(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "WebhookFailed",
                Notes = "Webhook failed",
                DataJson = JsonSerializer.Serialize(new { endpoint = demoWebhookUrl, error = "Timeout" }),
                CreatedAt = createdAt.AddDays(random.Next(1, 10))
            });
        }

        var eventStatus = i % 6 == 0 ? "Failed" : i % 4 == 0 ? "Pending" : "Sent";
        var attempts = eventStatus == "Failed" ? random.Next(2, 5) : eventStatus == "Sent" ? 1 : 0;
        var lastError = eventStatus == "Failed" ? "HTTP 500 from target" : null;
        var baseEventTime = createdAt.AddHours(random.Next(2, 36));

        automationEvents.Add(BuildAutomationEvent(lead, "LeadCreated", eventStatus, attempts, lastError, baseEventTime));

        if (i % 3 == 0)
        {
            automationEvents.Add(BuildAutomationEvent(
                lead,
                "LeadScored",
                eventStatus == "Sent" ? "Sent" : "Pending",
                eventStatus == "Sent" ? 1 : attempts,
                eventStatus == "Failed" ? lastError : null,
                baseEventTime.AddHours(6)));
        }

        if (i % 2 == 0)
        {
            automationEvents.Add(BuildAutomationEvent(
                lead,
                "StatusChanged",
                eventStatus,
                attempts,
                lastError,
                baseEventTime.AddHours(3)));
        }
    }

    dbContext.Leads.AddRange(leads);
    dbContext.LeadActivities.AddRange(activities);
    dbContext.AutomationEvents.AddRange(automationEvents);
    await dbContext.SaveChangesAsync();
}

