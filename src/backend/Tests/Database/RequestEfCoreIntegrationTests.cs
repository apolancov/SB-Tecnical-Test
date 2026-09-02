using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Database;

public class RequestEfCoreIntegrationTests
{
    private static ApplicationDbContext CreateSqliteContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Request_CanBeCreatedAndReadBackThroughEfCoreSqlite()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var (requester, area, type) = SeedDomain();

        using (var context = CreateSqliteContext(connection))
        {
            context.Database.EnsureCreated();

            context.Users.Add(requester);
            context.Areas.Add(area);
            context.RequestTypes.Add(type);
            context.SaveChanges();

            var request = new Request(
                code: "SOL-2026-0001",
                title: "PC no enciende",
                description: "La PC del puesto 12 no enciende.",
                priority: RequestPriority.High,
                requester: requester,
                area: area,
                requestType: type,
                createdAt: DateTime.UtcNow,
                dueDate: DateTime.UtcNow.AddDays(3));

            context.Requests.Add(request);
            context.SaveChanges();
        }

        using (var context = CreateSqliteContext(connection))
        {
            var stored = context.Requests
                .Include(r => r.Requester)
                .Include(r => r.Area)
                .Include(r => r.RequestType)
                .Include(r => r.StatusHistory)
                .Single(r => r.Code == "SOL-2026-0001");

            Assert.Equal("PC no enciende", stored.Title);
            Assert.Equal(RequestPriority.High, stored.Priority);
            Assert.Equal(RequestStatus.Submitted, stored.Status);
            Assert.Equal(area.Id, stored.AreaId);
            Assert.Equal(type.Id, stored.RequestTypeId);
            Assert.Equal(requester.Id, stored.RequesterId);
            Assert.Single(stored.StatusHistory);
        }
    }

    [Fact]
    public void Request_UniqueCodeIndex_RejectsDuplicateInsertion()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var (requester, area, type) = SeedDomain();

        using (var context = CreateSqliteContext(connection))
        {
            context.Database.EnsureCreated();

            context.Users.Add(requester);
            context.Areas.Add(area);
            context.RequestTypes.Add(type);

            var request = new Request(
                code: "SOL-2026-0001",
                title: "First",
                description: string.Empty,
                priority: RequestPriority.Low,
                requester: requester,
                area: area,
                requestType: type,
                createdAt: DateTime.UtcNow,
                dueDate: null);

            context.Requests.Add(request);
            context.SaveChanges();

            var duplicate = new Request(
                code: "SOL-2026-0001",
                title: "Second",
                description: string.Empty,
                priority: RequestPriority.Medium,
                requester: requester,
                area: area,
                requestType: type,
                createdAt: DateTime.UtcNow,
                dueDate: null);

            context.Requests.Add(duplicate);
            Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        }
    }

    [Fact]
    public void Request_ChangeStatusAppendsHistoryEntry()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var (requester, area, type) = SeedDomain();

        using (var context = CreateSqliteContext(connection))
        {
            context.Database.EnsureCreated();
            context.Users.Add(requester);
            context.Areas.Add(area);
            context.RequestTypes.Add(type);
            context.SaveChanges();

            var request = new Request(
                code: "SOL-2026-0001",
                title: "Title",
                description: string.Empty,
                priority: RequestPriority.Medium,
                requester: requester,
                area: area,
                requestType: type,
                createdAt: DateTime.UtcNow,
                dueDate: null);

            request.ChangeStatus(RequestStatus.InReview, requester, "Triaged.", DateTime.UtcNow.AddHours(1));
            request.AddComment(requester, "Will investigate.", CommentVisibility.Internal, DateTime.UtcNow.AddHours(2));

            context.Requests.Add(request);
            context.SaveChanges();
        }

        using (var context = CreateSqliteContext(connection))
        {
            var stored = context.Requests
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
                .Single(r => r.Code == "SOL-2026-0001");

            Assert.Equal(RequestStatus.InReview, stored.Status);
            Assert.Equal(2, stored.StatusHistory.Count);
            Assert.Single(stored.Comments);
            Assert.Equal(CommentVisibility.Internal, stored.Comments[0].Visibility);
        }
    }

    private static (User requester, Area area, RequestType type) SeedDomain()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        return (requester, area, type);
    }
}
