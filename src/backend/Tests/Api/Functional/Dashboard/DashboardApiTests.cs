using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Functional;
using Application.Authentication.AuthenticateUser;
using Application.Requests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Functional.Dashboard;

public class DashboardApiTests : IClassFixture<DashboardApiTests.Factory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AdminUsername = "admin";
    private const string AdminPassword = "AdminPass123!";
    private const string AgentUsername = "agent";
    private const string AgentPassword = "AgentPass123!";

    private readonly Factory _factory;

    public DashboardApiTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSummary_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/resumen");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_ReturnsExpectedShapeWithEmptyDatabase()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/dashboard/resumen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content
            .ReadFromJsonAsync<DashboardSummaryResponse>(JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(0, summary!.TotalRequests);
        Assert.Equal(0, summary.PendingRequests);
        Assert.Equal(0, summary.AssignedRequests);
        Assert.Equal(0, summary.UnassignedRequests);
        Assert.Equal(0, summary.OverdueRequests);
        Assert.NotNull(summary.RecentRequests);
        Assert.Empty(summary.RecentRequests);
        Assert.NotNull(summary.RequestsByStatus);
        Assert.NotNull(summary.RequestsByPriority);

        foreach (RequestStatus status in Enum.GetValues<RequestStatus>())
        {
            Assert.True(summary.RequestsByStatus.ContainsKey(status.ToString()));
        }

        foreach (RequestPriority priority in Enum.GetValues<RequestPriority>())
        {
            Assert.True(summary.RequestsByPriority.ContainsKey(priority.ToString()));
        }
    }

    [Fact]
    public async Task GetSummary_AfterCreatingAndAssigningRequests_ReflectsMetrics()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var requestId = await CreateRequestAsync(client);
        var requestId2 = await CreateRequestAsync(client);

        var agentId = _factory.GetAgentId();

        var assignResponse = await client.PatchAsJsonAsync(
            $"/api/solicitudes/{requestId}/asignacion",
            new { responsibleUserId = agentId, comment = "Take it." });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var response = await client.GetAsync("/api/dashboard/resumen");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content
            .ReadFromJsonAsync<DashboardSummaryResponse>(JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(2, summary!.TotalRequests);
        Assert.Equal(2, summary.PendingRequests);
        Assert.Equal(1, summary.AssignedRequests);
        Assert.Equal(1, summary.UnassignedRequests);

        Assert.Equal(2, summary.RequestsByStatus["Submitted"]);
        Assert.Equal(0, summary.RequestsByStatus["Closed"]);

        Assert.Equal(2, summary.RequestsByPriority["High"]);
    }

    private async Task<Guid> CreateRequestAsync(HttpClient client)
    {
        var (areaId, requestTypeId) = _factory.GetCatalogIds();
        var title = "Dashboard request " + Guid.NewGuid().ToString("N");

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            title,
            description = "Dashboard description",
            priority = "High",
            areaId,
            requestTypeId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<Application.Requests.RequestDto>(JsonOptions))!;
        return dto.Id;
    }

    public sealed record DashboardSummaryResponse(
        int TotalRequests,
        Dictionary<string, int> RequestsByStatus,
        Dictionary<string, int> RequestsByPriority,
        int OverdueRequests,
        int PendingRequests,
        int AssignedRequests,
        int UnassignedRequests,
        IReadOnlyList<RecentRequestItem> RecentRequests,
        DateTime GeneratedAtUtc);

    public sealed record RecentRequestItem(
        Guid Id,
        string Code,
        string Title,
        RequestPriority Priority,
        RequestStatus Status,
        DateTime CreatedAt,
        DateTime? DueDate,
        Guid RequesterId,
        string RequesterUsername,
        Guid? ResponsibleId,
        string? ResponsibleUsername);

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = TestTokenFactory.TestIssuer,
                    ["Jwt:Audience"] = TestTokenFactory.TestAudience,
                    ["Jwt:SecretKey"] = TestTokenFactory.TestSecretKey,
                    ["Jwt:ExpirationMinutes"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                RemoveDbContextRegistrations(services);
                OverrideJwtSettings(services);
                OverrideRequestCodeGenerator(services);
                RegisterSqliteDatabase(services);
                SeedDatabase(services);
            });
        }

        private static void RemoveDbContextRegistrations(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
        }

        private static void OverrideJwtSettings(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(JwtSettings));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(new JwtSettings
            {
                Issuer = TestTokenFactory.TestIssuer,
                Audience = TestTokenFactory.TestAudience,
                SecretKey = TestTokenFactory.TestSecretKey,
                ExpirationMinutes = 60
            });
        }

        private static void OverrideRequestCodeGenerator(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Application.Requests.IRequestCodeGenerator));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<Application.Requests.IRequestCodeGenerator, TestRequestCodeGenerator>();
        }

        private static void RegisterSqliteDatabase(IServiceCollection services)
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            services.AddSingleton(connection);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connection));
        }

        private static void SeedDatabase(IServiceCollection services)
        {
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                var hasher = new Infrastructure.Authentication.PasswordHasher();
                context.Users.AddRange(
                    new User(AdminUsername, "admin@example.local", hasher.Hash(AdminPassword), UserRole.Admin),
                    new User(AgentUsername, "agent@example.local", hasher.Hash(AgentPassword), UserRole.User));
                context.SaveChanges();
            }

            if (!context.Areas.Any())
            {
                context.Areas.AddRange(
                    new Area("Atención al Ciudadano"),
                    new Area("Soporte Técnico"));
                context.SaveChanges();
            }

            if (!context.RequestTypes.Any())
            {
                context.RequestTypes.AddRange(
                    new RequestType("Incidente", "Reporte de un incidente."),
                    new RequestType("Requerimiento", "Solicitud."));
                context.SaveChanges();
            }
        }

        public (Guid areaId, Guid requestTypeId) GetCatalogIds()
        {
            using var scope = Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return (context.Areas.OrderBy(a => a.Name).First().Id,
                    context.RequestTypes.OrderBy(t => t.Name).First().Id);
        }

        public Guid GetAgentId()
        {
            using var scope = Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return context.Users.Single(u => u.Username == AgentUsername).Id;
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync(
            string username = AdminUsername,
            string password = AdminPassword)
        {
            var client = CreateClient();
            var loginResponse = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username, password });

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            var body = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
            Assert.NotNull(body);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", body!.AccessToken);

            return client;
        }
    }

    internal sealed class TestRequestCodeGenerator : Application.Requests.IRequestCodeGenerator
    {
        private static long _counter;

        public Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
        {
            var next = Interlocked.Increment(ref _counter);
            var year = DateTime.UtcNow.Year;
            return Task.FromResult($"SOL-{year:D4}-{next:D4}");
        }
    }
}