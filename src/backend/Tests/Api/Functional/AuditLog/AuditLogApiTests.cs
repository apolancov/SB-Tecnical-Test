using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api;
using Application.Common.Pagination;
using Application.Authentication.AuthenticateUser;
using Application.Common.Authentication;
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

namespace Api.Functional.AuditLog;

public class AuditLogApiTests : IClassFixture<AuditLogApiTests.Factory>
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "AdminPass123!";
    private const string RegularUsername = "user";
    private const string RegularPassword = "UserPass123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Factory _factory;

    public AuditLogApiTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auditoria");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithRegularUserToken_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auditoria");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithAdminToken_AfterLogin_ReturnsLoginSucceededEntries()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auditoria?action=LoginSucceeded");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadPageAsync(response);
        Assert.True(page.TotalItems >= 1, "Expected at least one LoginSucceeded audit entry.");
        Assert.All(page.Items, item => Assert.Equal(AuditAction.LoginSucceeded, item.Action));
    }

    [Fact]
    public async Task List_FilterByActorUserId_ReturnsMatchingEntries()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var page = await ReadPageAsync(await client.GetAsync("/api/auditoria?action=LoginSucceeded"));
        Assert.NotEmpty(page.Items);
        var actorId = page.Items[0].ActorUserId;
        Assert.NotNull(actorId);

        var filtered = await ReadPageAsync(
            await client.GetAsync($"/api/auditoria?action=LoginSucceeded&actorUserId={actorId}"));

        Assert.NotEmpty(filtered.Items);
        Assert.All(filtered.Items, item => Assert.Equal(actorId, item.ActorUserId));
    }

    [Fact]
    public async Task List_WithInvalidPage_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auditoria?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_WithFromDateAfterToDate_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            "/api/auditoria?fromDate=2026-12-01T00:00:00Z&toDate=2026-01-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/auditoria/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithKnownId_ReturnsEntry()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var page = await ReadPageAsync(await client.GetAsync("/api/auditoria?action=LoginSucceeded"));
        var firstId = page.Items[0].Id;

        var response = await client.GetAsync($"/api/auditoria/{firstId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Application.AuditLog.AuditLogEntryDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(firstId, body!.Id);
    }

    [Fact]
    public async Task AuthorizationDenied_IsLoggedWhenNonAdminHitsAuditoria()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var forbidden = await client.GetAsync("/api/auditoria");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var adminClient = _factory.CreateClient();
        var adminToken = await LoginAsync(adminClient, AdminUsername, AdminPassword);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var page = await ReadPageAsync(
            await adminClient.GetAsync("/api/auditoria?action=AuthorizationDenied"));
        Assert.NotEmpty(page.Items);
        var entry = page.Items[0];
        Assert.Equal(AuditOutcome.Denied, entry.Outcome);
        Assert.Equal("AdminOnly", entry.EntityType);
        Assert.Contains("GET /api/auditoria", entry.EntityId);
    }

    private static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        Assert.NotNull(body);
        return body!.AccessToken;
    }

    private static async Task<PaginatedResponse<Application.AuditLog.AuditLogEntryDto>> ReadPageAsync(
        HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<PaginatedResponse<Application.AuditLog.AuditLogEntryDto>>(JsonOptions))!;
    }

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
            var settingsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(JwtSettings));
            if (settingsDescriptor is not null)
            {
                services.Remove(settingsDescriptor);
            }

            var testSettings = new JwtSettings
            {
                Issuer = TestTokenFactory.TestIssuer,
                Audience = TestTokenFactory.TestAudience,
                SecretKey = TestTokenFactory.TestSecretKey,
                ExpirationMinutes = 60
            };

            services.AddSingleton(testSettings);
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
                    new User(RegularUsername, "user@example.local", hasher.Hash(RegularPassword), UserRole.User));
                context.SaveChanges();
            }
        }
    }
}