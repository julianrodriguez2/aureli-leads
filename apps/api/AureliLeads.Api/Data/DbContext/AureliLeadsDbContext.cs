using AureliLeads.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AureliLeads.Api.Data.DbContext;

public sealed class AureliLeadsDbContext : DbContext
{
    public AureliLeadsDbContext(DbContextOptions<AureliLeadsDbContext> options) : base(options)
    {
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<AutomationEvent> AutomationEvents => Set<AutomationEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<SettingsActivity> SettingsActivities => Set<SettingsActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: configure entity mappings, constraints, and indexes.
        base.OnModelCreating(modelBuilder);
    }
}
