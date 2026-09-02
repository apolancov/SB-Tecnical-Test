using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api;
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

namespace Api.Functional.Auth;

public class AuthApiTests : IClassFixture<AuthApiTests.Factory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AdminUsername = "admin";
    private const string AdminEmail = "admin@example.local";
    private const string AdminPassword = "AdminPass123!";
    private const string RegularUsername = "user";
    private const string RegularEmail = "user@example.local";
    private const string RegularPassword = "UserPass123!";
    private const string DisabledUsername = "disabled";
    private const string DisabledEmail = "disabled@example.local";
    private const string DisabledPassword = "DisabledPass123!";

    private readonly Factory _factory;

    public AuthApiTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsTokenAndUserInfo()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = AdminUsername, password = AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadResponseAsync(response);
        Assert.NotNull(body);
        Assert.Equal("Bearer", body!.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.True(body.ExpiresAt > DateTime.UtcNow.AddMinutes(30));
        Assert.NotNull(body.User);
        Assert.Equal(AdminUsername, body.User!.Username);
        Assert.Equal(UserRole.Admin, body.User.Role);
        Assert.NotEqual(Guid.Empty, body.User.Id);
    }

    [Fact]
    public async Task Login_WithValidRegularUserCredentials_ReturnsUserRole()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = RegularUsername, password = RegularPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadResponseAsync(response);
        Assert.NotNull(body);
        Assert.Equal(UserRole.User, body!.User.Role);
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "ghost", password = AdminPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = AdminUsername, password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithDisabledUser_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = DisabledUsername, password = DisabledPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("", AdminPassword)]
    [InlineData(AdminUsername, "")]
    public async Task Login_WithMissingCredentials_ReturnsBadRequest(string username, string password)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserDto>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal(AdminUsername, user!.Username);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task Solicitudes_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Solicitudes_WithValidAdminToken_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Solicitudes_WithValidRegularUserToken_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Solicitudes_WithInvalidBearerToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.token");

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Solicitudes_WithExpiredToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var expiredToken = TestTokenFactory.CreateExpiredToken(
            Guid.NewGuid(),
            AdminUsername,
            "Admin");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Solicitudes_WithTokenSignedWithDifferentKey_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var forgedKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("ANOTHER_SECRET_KEY_AT_LEAST_THIRTYTWO_CHARACTERS_LONG"));

        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            forgedKey,
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: TestTokenFactory.TestIssuer,
            audience: TestTokenFactory.TestAudience,
            claims: new[]
            {
                new System.Security.Claims.Claim(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName,
                    AdminUsername),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role,
                    "Admin")
            },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        var forged = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminHealth_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/admin/health");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminHealth_WithRegularUserToken_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/admin/health");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminHealth_WithAdminToken_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/admin/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadResponseAsync(response);
        return body!.AccessToken;
    }

    private static async Task<AuthenticationResponse?> ReadResponseAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
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
                context.Users.AddRange(
                    new User(AdminUsername, AdminEmail, SeedPasswordHash(AdminPassword), UserRole.Admin),
                    new User(RegularUsername, RegularEmail, SeedPasswordHash(RegularPassword), UserRole.User),
                    new User(DisabledUsername, DisabledEmail, SeedPasswordHash(DisabledPassword), UserRole.User, isActive: false));

                context.SaveChanges();
            }
        }

        private static string SeedPasswordHash(string password)
        {
            var hasher = new Infrastructure.Authentication.PasswordHasher();
            return hasher.Hash(password);
        }
    }
}
