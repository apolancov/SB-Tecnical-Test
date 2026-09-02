using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Domain.AuditLog;

public class AuditLogEntryTests
{
    private static readonly DateTime FixedTimestamp =
        new(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc);

    private static AuditLogEntry CreateEntry(
        AuditAction action = AuditAction.RequestCreated,
        AuditOutcome outcome = AuditOutcome.Success,
        string entityType = "Request",
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        Guid? actorUserId = null,
        string? actorUserName = null,
        DateTime? timestamp = null)
    {
        return new AuditLogEntry(
            action: action,
            outcome: outcome,
            entityType: entityType,
            entityId: entityId,
            details: details,
            ipAddress: ipAddress,
            actorUserId: actorUserId,
            actorUserName: actorUserName,
            timestamp: timestamp);
    }

    [Fact]
    public void Constructor_WithValidArguments_AssignsProperties()
    {
        var entry = CreateEntry(
            entityId: Guid.NewGuid().ToString(),
            details: "Request 'SOL-2026-0001' created.",
            ipAddress: "192.0.2.10",
            actorUserId: Guid.NewGuid(),
            actorUserName: "admin");

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(AuditAction.RequestCreated, entry.Action);
        Assert.Equal(AuditOutcome.Success, entry.Outcome);
        Assert.Equal("Request", entry.EntityType);
        Assert.Equal("Request 'SOL-2026-0001' created.", entry.Details);
        Assert.Equal("192.0.2.10", entry.IpAddress);
        Assert.NotNull(entry.ActorUserName);
        Assert.Equal("admin", entry.ActorUserName);
        Assert.NotNull(entry.ActorUserId);
        Assert.NotEqual(Guid.Empty, entry.ActorUserId);
    }

    [Fact]
    public void Constructor_TrimsWhitespaceOnAllStringProperties()
    {
        var entry = CreateEntry(
            entityId: "  abc-123  ",
            details: "  details  ",
            ipAddress: "  10.0.0.1  ",
            actorUserName: "  admin  ");

        Assert.Equal("abc-123", entry.EntityId);
        Assert.Equal("details", entry.Details);
        Assert.Equal("10.0.0.1", entry.IpAddress);
        Assert.Equal("admin", entry.ActorUserName);
    }

    [Fact]
    public void Constructor_DefaultsTimestampToUtcNow()
    {
        var before = DateTime.UtcNow;
        var entry = CreateEntry();
        var after = DateTime.UtcNow;

        Assert.InRange(entry.Timestamp, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    [Fact]
    public void Constructor_ConvertsProvidedTimestampToUtc()
    {
        var local = new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Local);
        var entry = CreateEntry(timestamp: local);

        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    [Fact]
    public void Constructor_NullOptionals_AreStoredAsNull()
    {
        var entry = CreateEntry();

        Assert.Null(entry.EntityId);
        Assert.Null(entry.Details);
        Assert.Null(entry.IpAddress);
        Assert.Null(entry.ActorUserId);
        Assert.Null(entry.ActorUserName);
    }

    [Fact]
    public void Constructor_EmptyActorUserId_StoredAsNull()
    {
        var entry = CreateEntry(actorUserId: Guid.Empty);

        Assert.Null(entry.ActorUserId);
    }

    [Fact]
    public void Constructor_EmptyOrWhitespaceEntityType_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateEntry(entityType: string.Empty));
        Assert.Throws<ArgumentException>(() => CreateEntry(entityType: "   "));
    }

    [Fact]
    public void Constructor_EntityTypeTooLong_Throws()
    {
        var entityType = new string('a', AuditLogEntry.EntityTypeMaximumLength + 1);

        Assert.Throws<ArgumentException>(() => CreateEntry(entityType: entityType));
    }

    [Fact]
    public void Constructor_DetailsTooLong_Throws()
    {
        var details = new string('d', AuditLogEntry.DetailsMaximumLength + 1);

        Assert.Throws<ArgumentException>(() => CreateEntry(details: details));
    }

    [Fact]
    public void Constructor_InvalidAction_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateEntry(action: (AuditAction)9999));
    }

    [Fact]
    public void Constructor_InvalidOutcome_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateEntry(outcome: (AuditOutcome)9999));
    }
}