using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api;
using Application.Authentication.AuthenticateUser;
using Application.Common.Authentication;
using Application.Common.Pagination;
using Application.Users;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Functional.Users;

public class UsersApiTests : IClassFixture<UsersApiTests.Factory>
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

    public UsersApiTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithRegularUserToken_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithAdminToken_ReturnsSeededUsers()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadPageAsync(response);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task List_FilterByRole_ReturnsMatchingUsers()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users?role=User");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadPageAsync(response);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(UserRole.User, page.Items[0].Role);
    }

    [Fact]
    public async Task List_WithInvalidPage_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/users?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithRegularUserToken_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("users.read.not_found", problem!.Type);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username = "new.user",
            email = "new.user@example.local",
            password = "NewUserPass123!",
            role = "Analista",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithRegularUserToken_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username = "new.user",
            email = "new.user@example.local",
            password = "NewUserPass123!",
            role = "Analista",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidPayload_ReturnsCreated()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueUsername = "created." + Guid.NewGuid().ToString("N").Substring(0, 8);
        var uniqueEmail = $"{uniqueUsername}@example.local";
        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username = uniqueUsername,
            email = uniqueEmail,
            password = "CreatedUserPass123!",
            role = "Analista",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var dto = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(uniqueUsername, dto!.Username);
        Assert.Equal(uniqueEmail, dto.Email);
        Assert.Equal(UserRole.Analista, dto.Role);
        Assert.True(dto.IsActive);
        Assert.NotEqual(Guid.Empty, dto.Id);
    }

    [Fact]
    public async Task Create_WithDuplicateUsername_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username = "admin",
            email = "duplicate@example.local",
            password = "NewUserPass123!",
            role = "Analista",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("users.create.username.conflict", problem!.Type);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username = "duplicate.user",
            email = "admin@example.local",
            password = "NewUserPass123!",
            role = "Analista",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("users.create.email.conflict", problem!.Type);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username = "  ",
            email = "no-at-sign",
            password = "short",
            role = "Analista",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", new
        {
            username = "renamed",
            email = "renamed@example.local",
            role = "User",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidPayload_UpdatesUser()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await client.PostAsJsonAsync("/api/users", new
        {
            username = "to.rename",
            email = "to.rename@example.local",
            password = "RenameUserPass123!",
            role = "Analista",
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = (await created.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;

        var newUsername = "renamed." + Guid.NewGuid().ToString("N").Substring(0, 8);
        var response = await client.PutAsJsonAsync($"/api/users/{original.Id}", new
        {
            username = newUsername,
            email = "renamed@example.local",
            role = "User",
            isActive = false,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(newUsername, updated.Username);
        Assert.Equal(UserRole.User, updated.Role);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ChangePassword_WithUnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync(
            $"/api/users/{Guid.NewGuid()}/password",
            new { newPassword = "NewPassword123!" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithValidPayload_ReturnsNoContent()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await client.PostAsJsonAsync("/api/users", new
        {
            username = "to.chpwd",
            email = "to.chpwd@example.local",
            password = "ChPwdUserPass123!",
            role = "Analista",
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = (await created.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;

        var response = await client.PutAsJsonAsync(
            $"/api/users/{original.Id}/password",
            new { newPassword = "ChangedPassword123!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithShortPassword_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync(
            $"/api/users/{Guid.NewGuid()}/password",
            new { newPassword = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await client.PostAsJsonAsync("/api/users", new
        {
            username = "to.deactivate",
            email = "to.deactivate@example.local",
            password = "DeactivateUserPass123!",
            role = "Analista",
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = (await created.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;

        var response = await client.DeleteAsync($"/api/users/{original.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithRegularUserToken_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, RegularUsername, RegularPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAlreadyInactiveUser_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var token = await LoginAsync(client, AdminUsername, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await client.PostAsJsonAsync("/api/users", new
        {
            username = "to.deactivate.twice",
            email = "to.deactivate.twice@example.local",
            password = "DeactivateUserPass123!",
            role = "Analista",
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = (await created.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;

        var firstDelete = await client.DeleteAsync($"/api/users/{original.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);

        var secondDelete = await client.DeleteAsync($"/api/users/{original.Id}");
        Assert.Equal(HttpStatusCode.Conflict, secondDelete.StatusCode);
        var problem = await secondDelete.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("users.delete.already_inactive", problem!.Type);
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

    private static async Task<PaginatedResponse<UserDto>> ReadPageAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<PaginatedResponse<UserDto>>(JsonOptions))!;
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
