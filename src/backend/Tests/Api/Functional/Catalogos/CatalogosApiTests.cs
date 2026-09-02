using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Functional;
using Application.Authentication.AuthenticateUser;
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

namespace Api.Functional.Catalogos;

public class CatalogosApiTests : IClassFixture<CatalogosApiTests.Factory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AdminUsername = "admin";
    private const string AdminPassword = "AdminPass123!";

    private readonly Factory _factory;

    public CatalogosApiTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListAreas_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/catalogos/areas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListAreas_ReturnsOnlyActiveAreas()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalogos/areas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var areas = await response.Content
            .ReadFromJsonAsync<List<AreaCatalogItem>>(JsonOptions);

        Assert.NotNull(areas);
        Assert.NotEmpty(areas!);

        foreach (var area in areas!)
        {
            Assert.NotEqual(Guid.Empty, area.Id);
            Assert.False(string.IsNullOrWhiteSpace(area.Name));
        }

        Assert.Contains(areas, a => a.Name == "Atención al Ciudadano");
        Assert.Contains(areas, a => a.Name == "Soporte Técnico");
        Assert.DoesNotContain(areas, a => a.Name == "Área Inactiva");

        var orderedNames = areas.Select(a => a.Name).ToList();
        var sortedNames = orderedNames.OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(sortedNames, orderedNames);
    }

    [Fact]
    public async Task ListRequestTypes_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/catalogos/tipos-solicitud");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListRequestTypes_ReturnsOnlyActiveRequestTypes()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/catalogos/tipos-solicitud");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var types = await response.Content
            .ReadFromJsonAsync<List<RequestTypeCatalogItem>>(JsonOptions);

        Assert.NotNull(types);
        Assert.NotEmpty(types!);

        foreach (var type in types!)
        {
            Assert.NotEqual(Guid.Empty, type.Id);
            Assert.False(string.IsNullOrWhiteSpace(type.Name));
        }

        Assert.Contains(types, t => t.Name == "Incidente");
        Assert.Contains(types, t => t.Name == "Requerimiento");
        Assert.DoesNotContain(types, t => t.Name == "Tipo Inactivo");

        var orderedNames = types.Select(t => t.Name).ToList();
        var sortedNames = orderedNames.OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(sortedNames, orderedNames);
    }

    public sealed record AreaCatalogItem(Guid Id, string Name);

    public sealed record RequestTypeCatalogItem(Guid Id, string Name, string Description);

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
                    new User(AdminUsername, "admin@example.local", hasher.Hash(AdminPassword), UserRole.Admin));
                context.SaveChanges();
            }

            if (!context.Areas.Any())
            {
                context.Areas.AddRange(
                    new Area("Atención al Ciudadano"),
                    new Area("Soporte Técnico"),
                    new Area("Mantenimiento"),
                    new Area("Área Inactiva", isActive: false));
                context.SaveChanges();
            }

            if (!context.RequestTypes.Any())
            {
                context.RequestTypes.AddRange(
                    new RequestType("Incidente", "Reporte de un incidente."),
                    new RequestType("Requerimiento", "Solicitud de requerimiento."),
                    new RequestType("Tipo Inactivo", "Tipo inactivo.", isActive: false));
                context.SaveChanges();
            }
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
}