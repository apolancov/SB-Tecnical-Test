using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api;
using Application.Authentication.AuthenticateUser;
using Application.Common.Authentication;
using Application.Common.Pagination;
using Application.Institutions.GetInstitutionFilterOptions;
using Application.Institutions.GetInstitutions;
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

namespace Api.Functional;

public class InstitutionsApiTests : IClassFixture<InstitutionsApiTests.Factory>
{
    private const int SeededInstitutionCount = 181;
    private const string DefaultUsername = "admin";
    private const string DefaultPassword = "AdminPass123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Factory _factory;

    public InstitutionsApiTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetInstitutions_WithoutToken_IsPublicAndReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/institutions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(SeededInstitutionCount, page.TotalItems);
        Assert.Equal(20, page.Items.Count);
        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task GetInstitutions_ReturnsSeededInstitutions()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(SeededInstitutionCount, page.TotalItems);
        Assert.Equal(10, page.TotalPages);
        Assert.Equal(20, page.Items.Count);
        Assert.NotEmpty(page.Items);

        var first = page.Items[0];
        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.False(string.IsNullOrWhiteSpace(first.Name));
        Assert.False(string.IsNullOrWhiteSpace(first.Category));
        Assert.False(string.IsNullOrWhiteSpace(first.StatePower));
        Assert.False(string.IsNullOrWhiteSpace(first.Sector));
    }

    [Fact]
    public async Task GetInstitutions_WithPageSize_ReturnsAtMostThatManyRecords()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Equal(1, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(SeededInstitutionCount, page.TotalItems);
    }

    [Fact]
    public async Task GetInstitutions_SecondPage_ReturnsCorrectSlice()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var firstPage = await ReadPageAsync(
            await client.GetAsync("/api/institutions?page=1&pageSize=10"));
        var secondPage = await ReadPageAsync(
            await client.GetAsync("/api/institutions?page=2&pageSize=10"));

        Assert.Equal(10, secondPage.Items.Count);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(SeededInstitutionCount, secondPage.TotalItems);

        var firstNames = firstPage.Items.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain(secondPage.Items[0].Name, firstNames);
    }

    [Fact]
    public async Task GetInstitutions_FilterByName_ReturnsMatchingInstitutions()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?name=Acuario");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item => Assert.Contains("Acuario", item.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetInstitutions_FilterByCategory_ReturnsOnlyCategoryMatches()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "/api/institutions?category=" + Uri.EscapeDataString("Ministerio"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item =>
            Assert.Equal("Ministerio", item.Category, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetInstitutions_FilterByStatePower_ReturnsOnlyStatePowerMatches()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "/api/institutions?statePower=" + Uri.EscapeDataString("Poder Ejecutivo"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Equal(SeededInstitutionCount, page.TotalItems);
        Assert.All(page.Items, item =>
            Assert.Equal("Poder Ejecutivo", item.StatePower, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetInstitutions_FilterBySector_ReturnsOnlySectorMatches()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "/api/institutions?sector=" + Uri.EscapeDataString("Cultura"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item =>
            Assert.Equal("Cultura", item.Sector, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetInstitutions_CombinedFilters_ReturnIntersection()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "/api/institutions?statePower=" + Uri.EscapeDataString("Poder Ejecutivo")
            + "&sector=" + Uri.EscapeDataString("Cultura"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item =>
        {
            Assert.Equal("Poder Ejecutivo", item.StatePower, StringComparer.Ordinal);
            Assert.Equal("Cultura", item.Sector, StringComparer.Ordinal);
        });
    }

    [Fact]
    public async Task GetInstitutions_InvalidPage_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?page=0&pageSize=20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.NotNull(problem.Detail);
    }

    [Fact]
    public async Task GetInstitutions_NegativePage_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?page=-5&pageSize=20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInstitutions_InvalidPageSize_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?page=1&pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
    }

    [Fact]
    public async Task GetInstitutions_PageSizeAboveMaximum_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?page=1&pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInstitutions_NoMatches_ReturnsOkWithEmptyPage()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?name=__no_such_institution__");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalItems);
        Assert.Equal(0, page.TotalPages);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }

    [Fact]
    public async Task GetInstitutions_PageBeyondLast_ReturnsOkWithEmptyItemsButTotalIsCorrect()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?page=99&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Empty(page.Items);
        Assert.Equal(SeededInstitutionCount, page.TotalItems);
        Assert.Equal(10, page.TotalPages);
    }

    [Fact]
    public async Task GetInstitutions_MaximumPageSize_IsAccepted()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions?page=1&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Equal(100, page.PageSize);
        Assert.Equal(100, page.Items.Count);
        Assert.Equal(SeededInstitutionCount, page.TotalItems);
    }

    [Fact]
    public async Task GetInstitutions_DoesNotExposeDomainEntityType()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions");
        var rawJson = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("CreatedAt", rawJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetFilterOptions_WithoutToken_IsPublicAndReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/institutions/filter-options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var options = await response.Content.ReadFromJsonAsync<InstitutionFilterOptionsDto>(JsonOptions);
        Assert.NotNull(options);
        Assert.NotEmpty(options!.Categories);
        Assert.NotEmpty(options.StatePowers);
        Assert.NotEmpty(options.Sectors);
    }

    [Fact]
    public async Task GetFilterOptions_ReturnsDistinctSortedValues()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/institutions/filter-options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var options = await response.Content.ReadFromJsonAsync<InstitutionFilterOptionsDto>(JsonOptions);
        Assert.NotNull(options);

        Assert.NotEmpty(options!.Categories);
        Assert.NotEmpty(options.StatePowers);
        Assert.NotEmpty(options.Sectors);

        Assert.Equal(options.Categories.OrderBy(value => value, StringComparer.Ordinal), options.Categories);
        Assert.Equal(options.StatePowers.OrderBy(value => value, StringComparer.Ordinal), options.StatePowers);
        Assert.Equal(options.Sectors.OrderBy(value => value, StringComparer.Ordinal), options.Sectors);

        Assert.DoesNotContain(string.Empty, options.Categories);
        Assert.DoesNotContain(string.Empty, options.StatePowers);
        Assert.DoesNotContain(string.Empty, options.Sectors);

        Assert.Contains("Poder Ejecutivo", options.StatePowers);
        Assert.Contains("Cultura", options.Sectors);
    }

    [Fact]
    public async Task GetFilterOptions_ValuesMatchSeededData()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var options = await client.GetFromJsonAsync<InstitutionFilterOptionsDto>(
            "/api/institutions/filter-options",
            JsonOptions);

        Assert.NotNull(options);

        var seed = SampleSeedProvider.LoadFromSeedSql();
        var expectedCategories = seed.Select(item => item.Category).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
        var expectedStatePowers = seed.Select(item => item.StatePower).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
        var expectedSectors = seed.Select(item => item.Sector).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();

        Assert.Equal(expectedCategories, options!.Categories);
        Assert.Equal(expectedStatePowers, options.StatePowers);
        Assert.Equal(expectedSectors, options.Sectors);
    }

    private static async Task<PaginatedResponse<InstitutionDto>> ReadPageAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<PaginatedResponse<InstitutionDto>>(JsonOptions))!;
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

        public async Task<HttpClient> CreateAuthenticatedClientAsync(
            string username = DefaultUsername,
            string password = DefaultPassword)
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
                    new User(DefaultUsername, "admin@example.local", hasher.Hash(DefaultPassword), UserRole.Admin),
                    new User("user", "user@example.local", hasher.Hash("UserPass123!"), UserRole.User));
                context.SaveChanges();
            }

            Seed(context);
        }

        internal static void Seed(ApplicationDbContext context)
        {
            if (context.Institutions.Any())
            {
                return;
            }

            EnsureClassificationLookupRows(context);

            var categoriesByName = context.Categories
                .ToDictionary(category => category.Name, StringComparer.Ordinal);
            var statePowersByName = context.StatePowers
                .ToDictionary(statePower => statePower.Name, StringComparer.Ordinal);
            var sectorsByName = context.Sectors
                .ToDictionary(sector => sector.Name, StringComparer.Ordinal);

            var institutions = SampleSeedProvider
                .LoadFromSeedSql()
                .Select(seed => new Institution(
                    seed.Name,
                    categoriesByName[seed.Category],
                    statePowersByName[seed.StatePower],
                    sectorsByName[seed.Sector]))
                .ToList();

            context.Institutions.AddRange(institutions);
            context.SaveChanges();
        }

        private static void EnsureClassificationLookupRows(ApplicationDbContext context)
        {
            if (!context.Categories.Any())
            {
                var categories = SampleSeedProvider
                    .LoadFromSeedSql()
                    .Select(seed => seed.Category)
                    .Distinct(StringComparer.Ordinal)
                    .Select(name => new Category(name))
                    .ToList();

                context.Categories.AddRange(categories);
            }

            if (!context.StatePowers.Any())
            {
                var statePowers = SampleSeedProvider
                    .LoadFromSeedSql()
                    .Select(seed => seed.StatePower)
                    .Distinct(StringComparer.Ordinal)
                    .Select(name => new StatePower(name))
                    .ToList();

                context.StatePowers.AddRange(statePowers);
            }

            if (!context.Sectors.Any())
            {
                var sectors = SampleSeedProvider
                    .LoadFromSeedSql()
                    .Select(seed => seed.Sector)
                    .Distinct(StringComparer.Ordinal)
                    .Select(name => new Sector(name))
                    .ToList();

                context.Sectors.AddRange(sectors);
            }

            context.SaveChanges();
        }
    }
}
