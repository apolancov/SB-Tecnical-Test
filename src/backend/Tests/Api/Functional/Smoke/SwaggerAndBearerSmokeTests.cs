using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

namespace Api.Functional.Smoke;

public class SwaggerAndBearerSmokeTests : IClassFixture<SwaggerAndBearerSmokeTests.Factory>
{
    private readonly Factory _factory;

    public SwaggerAndBearerSmokeTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerJson_DocumentsBearerSecurityScheme()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        Assert.True(root.TryGetProperty("components", out var components));
        Assert.True(components.TryGetProperty("securitySchemes", out var schemes));

        var foundBearer = false;
        foreach (var scheme in schemes.EnumerateObject())
        {
            var value = scheme.Value;
            if (value.TryGetProperty("type", out var typeElement)
                && typeElement.GetString() == "http"
                && value.TryGetProperty("scheme", out var schemeElement)
                && schemeElement.GetString() == "bearer")
            {
                foundBearer = true;
                Assert.True(value.TryGetProperty("bearerFormat", out var bearerFormat));
                Assert.Equal("JWT", bearerFormat.GetString());
                break;
            }
        }

        Assert.True(foundBearer, "Expected a Bearer HTTP security scheme in the OpenAPI document.");

        Assert.True(root.TryGetProperty("security", out var security));
        Assert.NotEqual(0, security.GetArrayLength());
    }

    [Fact]
    public async Task Login_ThenAccessProtectedEndpoint_ThenAdminPolicy_WorksEndToEnd()
    {
        using var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "AdminPass123!" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<AuthenticationResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        Assert.NotNull(body);
        Assert.Equal("Bearer", body!.TokenType);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body.AccessToken);

        var institutions = await client.GetAsync("/api/institutions");
        Assert.Equal(HttpStatusCode.OK, institutions.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var adminHealth = await client.GetAsync("/api/auth/admin/health");
        Assert.Equal(HttpStatusCode.OK, adminHealth.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_ExposesAllRequiredSpanishRoutes()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        Assert.True(root.TryGetProperty("paths", out var paths));

        string[] requiredPaths =
        {
            "/api/auth/login",
            "/api/solicitudes",
            "/api/solicitudes/{id}",
            "/api/solicitudes/{id}/estado",
            "/api/solicitudes/{id}/asignacion",
            "/api/solicitudes/{id}/comentarios",
            "/api/dashboard/resumen",
            "/api/catalogos/areas",
            "/api/catalogos/tipos-solicitud"
        };

        foreach (var path in requiredPaths)
        {
            Assert.True(
                ContainsPath(paths, path),
                $"Swagger document must include path '{path}'.");
        }

        AssertHasMethod(paths, "/api/solicitudes", "get");
        AssertHasMethod(paths, "/api/solicitudes", "post");
        AssertHasMethod(paths, "/api/solicitudes/{id}", "get");
        AssertHasMethod(paths, "/api/solicitudes/{id}/estado", "patch");
        AssertHasMethod(paths, "/api/solicitudes/{id}/asignacion", "patch");
        AssertHasMethod(paths, "/api/solicitudes/{id}/comentarios", "post");
        AssertHasMethod(paths, "/api/dashboard/resumen", "get");
        AssertHasMethod(paths, "/api/catalogos/areas", "get");
        AssertHasMethod(paths, "/api/catalogos/tipos-solicitud", "get");
        AssertHasMethod(paths, "/api/auth/login", "post");

        Assert.False(
            ContainsPath(paths, "/api/requests"),
            "Legacy /api/requests route must not be present in the OpenAPI document.");
    }

    private static bool ContainsPath(JsonElement paths, string path)
    {
        foreach (var property in paths.EnumerateObject())
        {
            if (string.Equals(property.Name, path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertHasMethod(JsonElement paths, string path, string method)
    {
        foreach (var property in paths.EnumerateObject())
        {
            if (string.Equals(property.Name, path, StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(
                    property.Value.TryGetProperty(method, out _),
                    $"Path '{path}' must support HTTP '{method}'.");
                return;
            }
        }

        Assert.True(false, $"Path '{path}' is missing from the OpenAPI document.");
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
                var settingsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(JwtSettings));
                if (settingsDescriptor is not null)
                {
                    services.Remove(settingsDescriptor);
                }

                services.AddSingleton(new JwtSettings
                {
                    Issuer = TestTokenFactory.TestIssuer,
                    Audience = TestTokenFactory.TestAudience,
                    SecretKey = TestTokenFactory.TestSecretKey,
                    ExpirationMinutes = 60
                });

                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbDescriptor is not null)
                {
                    services.Remove(dbDescriptor);
                }

                var connection = new SqliteConnection("Filename=:memory:");
                connection.Open();
                services.AddSingleton(connection);
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(connection));

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated();

                if (!context.Users.Any())
                {
                    var hasher = new Infrastructure.Authentication.PasswordHasher();
                    context.Users.Add(new User(
                        "admin",
                        "admin@example.local",
                        hasher.Hash("AdminPass123!"),
                        UserRole.Admin));
                    context.SaveChanges();
                }
            });
        }
    }
}
