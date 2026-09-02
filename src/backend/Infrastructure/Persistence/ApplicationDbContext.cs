using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<StatePower> StatePowers => Set<StatePower>();

    public DbSet<Sector> Sectors => Set<Sector>();

    public DbSet<Institution> Institutions => Set<Institution>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<RequestType> RequestTypes => Set<RequestType>();

    public DbSet<Request> Requests => Set<Request>();

    public DbSet<RequestStatusHistoryEntry> RequestStatusHistory => Set<RequestStatusHistoryEntry>();

    public DbSet<RequestComment> RequestComments => Set<RequestComment>();

    public DbSet<RequestNotification> RequestNotifications => Set<RequestNotification>();

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
