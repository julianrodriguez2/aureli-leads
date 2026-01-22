using AureliLeads.Api.Auth;
using AureliLeads.Api.Background;
using AureliLeads.Api.Data.DbContext;
using AureliLeads.Api.Data.Entities;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "NextJsDev";

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aureli Leads API",
        Version = "v1"
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

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHostedService<AutomationEventDispatcher>();

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

await ApplyMigrationsAsync(app.Services);
await SeedAdminAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    await SeedSampleLeadsAsync(app.Services);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static async Task ApplyMigrationsAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AureliLeadsDbContext>();
    await dbContext.Database.MigrateAsync();
}

static async Task SeedAdminAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AureliLeadsDbContext>();

    if (await dbContext.Users.AnyAsync())
    {
        return;
    }

    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    var adminUser = new User
    {
        Id = Guid.NewGuid(),
        Email = "admin@local.test",
        Role = "admin",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");

    dbContext.Users.Add(adminUser);
    await dbContext.SaveChangesAsync();
}

static async Task SeedSampleLeadsAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AureliLeadsDbContext>();

    if (await dbContext.Leads.AnyAsync())
    {
        return;
    }

    var random = new Random(42);
    var statuses = new[] { "New", "Contacted", "Qualified", "Disqualified" };
    var sources = new[] { "web", "google_ads", "referral", "n8n" };
    var firstNames = new[] { "Ava", "Mateo", "Priya", "Liam", "Noah", "Sophia", "Maya", "Ethan", "Zoe", "Lucas" };
    var lastNames = new[] { "Chen", "Silva", "Nair", "Johnson", "Garcia", "Patel", "Kim", "Singh", "Brown", "Nguyen" };
    var messages = new[]
    {
        "Interested in a demo next week.",
        "Looking to compare pricing tiers.",
        "Asked about onboarding timeline.",
        "Needs approval from leadership.",
        "Wants to automate follow-ups."
    };

    var leads = new List<Lead>();
    var activities = new List<LeadActivity>();
    var now = DateTime.UtcNow;

    for (var i = 0; i < 32; i++)
    {
        var firstName = firstNames[i % firstNames.Length];
        var lastName = lastNames[(i + 3) % lastNames.Length];
        var createdAt = now.AddDays(-random.Next(0, 30)).AddMinutes(-random.Next(0, 1440));
        var updatedAt = createdAt.AddHours(random.Next(1, 72));
        if (updatedAt > now)
        {
            updatedAt = now;
        }

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
            Phone = $"+1-555-01{random.Next(10, 99)}",
            Company = $"{lastName} & Co",
            Source = sources[random.Next(sources.Length)],
            Status = statuses[random.Next(statuses.Length)],
            Score = random.Next(0, 101),
            Message = messages[random.Next(messages.Length)],
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        leads.Add(lead);

        activities.Add(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            Type = "Created",
            Notes = "Seeded lead",
            CreatedAt = createdAt
        });
    }

    dbContext.Leads.AddRange(leads);
    dbContext.LeadActivities.AddRange(activities);
    await dbContext.SaveChangesAsync();
}
