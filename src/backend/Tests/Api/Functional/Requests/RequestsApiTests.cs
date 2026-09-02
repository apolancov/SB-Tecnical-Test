using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Functional;
using Application.Authentication.AuthenticateUser;
using Application.Common.Authentication;
using Application.Requests;
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

namespace Api.Functional.Requests;

public class RequestsApiTests : IClassFixture<RequestsApiTests.Factory>
{
    private const int ExpectedAreaCount = 6;
    private const int ExpectedRequestTypeCount = 6;

    private const string AdminUsername = "admin";
    private const string AdminPassword = "AdminPass123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Factory _factory;

    public RequestsApiTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListRequests_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListRequests_WithToken_ReturnsEmptyPageWhenNoMatchingRequestsExist()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/solicitudes?search=" + Guid.NewGuid().ToString("N"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.Equal(0, page.TotalItems);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task CreateRequest_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            title = "Sample",
            description = "Sample description",
            priority = "High",
            areaId = Guid.NewGuid(),
            requestTypeId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRequest_WithMissingTitle_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var (areaId, requestTypeId) = _factory.GetCatalogIds();

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            title = "   ",
            description = "Sample description",
            priority = "High",
            areaId,
            requestTypeId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.create.title.required", problem!.Type);
    }

    [Fact]
    public async Task CreateRequest_WithUnknownArea_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var (_, requestTypeId) = _factory.GetCatalogIds();

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            title = "Sample",
            description = "Sample description",
            priority = "High",
            areaId = Guid.NewGuid(),
            requestTypeId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.create.area.not_found", problem!.Type);
    }

    [Fact]
    public async Task CreateRequest_WithUnknownRequestType_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var (areaId, _) = _factory.GetCatalogIds();

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            title = "Sample",
            description = "Sample description",
            priority = "High",
            areaId,
            requestTypeId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.create.request_type.not_found", problem!.Type);
    }

    [Fact]
    public async Task CreateRequest_WithValidPayload_ReturnsCreatedAndLocation()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var (areaId, requestTypeId) = _factory.GetCatalogIds();

        var title = "Sample request " + Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            title,
            description = "Sample description",
            priority = "High",
            areaId,
            requestTypeId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var dto = await response.Content.ReadFromJsonAsync<RequestDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(title, dto!.Title);
        Assert.Equal(Domain.Enums.RequestStatus.Submitted, dto.Status);
        Assert.Equal(Domain.Enums.RequestPriority.High, dto.Priority);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.StartsWith("SOL-", dto.Code);
    }

    [Fact]
    public async Task GetRequestDetail_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/solicitudes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRequestDetail_WithUnknownId_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/solicitudes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.read.not_found", problem!.Type);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidTransition_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{requestId}/estado", new
        {
            newStatus = "Closed",
            comment = "Cannot close immediately."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.change_status.transition.invalid", problem!.Type);
    }

    [Fact]
    public async Task ChangeStatus_WithValidTransition_UpdatesStatusAndPersistsHistory()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{requestId}/estado", new
        {
            newStatus = "InReview",
            comment = "Triaged."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detailResponse = await client.GetAsync($"/api/solicitudes/{requestId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var detail = await detailResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(2, detail!.StatusHistory.Count);
        Assert.Equal(Domain.Enums.RequestStatus.Submitted, detail.StatusHistory[0].NewStatus);
        Assert.Equal(Domain.Enums.RequestStatus.InReview, detail.StatusHistory[1].NewStatus);
        Assert.Equal(Domain.Enums.RequestStatus.Submitted, detail.StatusHistory[1].PreviousStatus);
    }

    [Fact]
    public async Task AssignRequest_WithUnknownUser_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{requestId}/asignacion", new
        {
            responsibleUserId = Guid.NewGuid(),
            comment = "Try."
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.assign.responsible.not_found", problem!.Type);
    }

    [Fact]
    public async Task AssignRequest_WithInactiveUser_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);
        var inactiveId = _factory.GetInactiveUserId();

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{requestId}/asignacion", new
        {
            responsibleUserId = inactiveId,
            comment = "Try."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.assign.responsible.inactive", problem!.Type);
    }

    [Fact]
    public async Task AssignRequest_WithActiveUser_AssignsResponsible()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);
        var agentId = _factory.GetActiveAgentId();

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{requestId}/asignacion", new
        {
            responsibleUserId = agentId,
            comment = "Routing."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await (await client.GetAsync($"/api/solicitudes/{requestId}"))
            .Content.ReadFromJsonAsync<RequestDetailDto>(JsonOptions);

        Assert.NotNull(detail);
        Assert.NotNull(detail!.Responsible);
        Assert.Equal(agentId, detail.Responsible!.Id);
    }

    [Fact]
    public async Task AddComment_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/solicitudes/{Guid.NewGuid()}/comentarios", new
        {
            text = "Comment",
            visibility = "Requester"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_WithEmptyText_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var response = await client.PostAsJsonAsync($"/api/solicitudes/{requestId}/comentarios", new
        {
            text = "   ",
            visibility = "Requester"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("requests.comment.text.required", problem!.Type);
    }

    [Fact]
    public async Task AddComment_WithValidPayload_PersistsComment()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var response = await client.PostAsJsonAsync($"/api/solicitudes/{requestId}/comentarios", new
        {
            text = "Working on it.",
            visibility = "Internal"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comment = await response.Content.ReadFromJsonAsync<RequestCommentDto>(JsonOptions);
        Assert.NotNull(comment);
        Assert.Equal("Internal", comment!.Visibility.ToString());
    }

    [Fact]
    public async Task AddComment_AuthorIsResolvedFromCurrentUserNotBody()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var response = await client.PostAsJsonAsync($"/api/solicitudes/{requestId}/comentarios", new
        {
            text = "Noting the user as author.",
            visibility = "Internal"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comment = await response.Content.ReadFromJsonAsync<RequestCommentDto>(JsonOptions);
        Assert.NotNull(comment);
        Assert.Equal(AdminUsername, comment!.AuthorUsername);
    }

    [Fact]
    public async Task GetRequestDetail_AsRequester_OnlyReturnsRequesterVisibleComments()
    {
        using var requesterClient = await _factory.CreateAuthenticatedClientAsync(
            username: "requester", password: "RequesterPass123!");
        var requestId = await _factory.CreateRequestAsync(requesterClient);

        await requesterClient.PostAsJsonAsync($"/api/solicitudes/{requestId}/comentarios", new
        {
            text = "Public note.",
            visibility = "Requester"
        });

        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        await adminClient.PostAsJsonAsync($"/api/solicitudes/{requestId}/comentarios", new
        {
            text = "Internal note.",
            visibility = "Internal"
        });

        var adminDetail = await (await adminClient.GetAsync($"/api/solicitudes/{requestId}"))
            .Content.ReadFromJsonAsync<RequestDetailDto>(JsonOptions);
        Assert.NotNull(adminDetail);
        Assert.Equal(2, adminDetail!.Comments.Count);

        var requesterDetail = await (await requesterClient.GetAsync($"/api/solicitudes/{requestId}"))
            .Content.ReadFromJsonAsync<RequestDetailDto>(JsonOptions);
        Assert.NotNull(requesterDetail);
        Assert.Single(requesterDetail!.Comments);
        Assert.Equal("Public note.", requesterDetail.Comments[0].Text);
    }

    [Fact]
    public async Task CreateRequest_WithEvidenceUrl_PersistsEvidenceUrl()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var (areaId, requestTypeId) = _factory.GetCatalogIds();

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            title = "Request with evidence",
            description = "Description",
            priority = "High",
            areaId,
            requestTypeId,
            evidenceUrl = "https://example.com/evidence/abc"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RequestDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("https://example.com/evidence/abc", dto!.EvidenceUrl);
    }

    [Fact]
    public async Task UpdateRequest_AsAnalista_UpdatesEditableFields()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{requestId}", new
        {
            title = "Updated title",
            description = "Updated description",
            priority = "Low",
            evidenceUrl = "https://example.com/evidence/updated"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RequestDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("Updated title", dto!.Title);
        Assert.Equal("Updated description", dto.Description);
        Assert.Equal(Domain.Enums.RequestPriority.Low, dto.Priority);
        Assert.Equal("https://example.com/evidence/updated", dto.EvidenceUrl);
    }

    [Fact]
    public async Task ListRequests_WithSortByCode_ReturnsAscending()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var first = await _factory.CreateRequestAsync(client);
        var second = await _factory.CreateRequestAsync(client);

        var response = await client.GetAsync(
            "/api/solicitudes?sortBy=code&sortDirection=asc&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await ReadPageAsync(response);
        Assert.True(page.Items.Count >= 2);

        var ascendingCodes = page.Items
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .Select(item => item.Code)
            .ToList();
        var actualCodes = page.Items.Select(item => item.Code).ToList();
        Assert.Equal(ascendingCodes, actualCodes);
    }

    [Fact]
    public async Task ListRequests_WithDateRangeFilter_AppliesCreatedAtBounds()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-1).ToString("o");
        var to = today.AddDays(1).ToString("o");
        var response = await client.GetAsync(
            $"/api/solicitudes?fromDate={from}&toDate={to}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadPageAsync(response);
        Assert.Contains(page.Items, item => item.Id == requestId);

        var past = today.AddYears(-5).ToString("o");
        var farPast = today.AddYears(-4).ToString("o");
        var emptyResponse = await client.GetAsync(
            $"/api/solicitudes?fromDate={past}&toDate={farPast}");
        Assert.Equal(HttpStatusCode.OK, emptyResponse.StatusCode);
        var emptyPage = await ReadPageAsync(emptyResponse);
        Assert.Empty(emptyPage.Items);
    }

    [Fact]
    public async Task ReopenRequest_AsAdmin_ReopensClosedRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        var submittedToClosed = new[]
        {
            new { newStatus = "InReview", comment = "Triaged." },
            new { newStatus = "Assigned", comment = "Assigned." },
            new { newStatus = "InProgress", comment = "Working on it." },
            new { newStatus = "Resolved", comment = "Fixed." },
            new { newStatus = "Closed", comment = "Resolution: fixed the network." }
        };

        foreach (var transition in submittedToClosed)
        {
            var transitionResponse = await client.PatchAsJsonAsync(
                $"/api/solicitudes/{requestId}/estado",
                transition);
            Assert.Equal(HttpStatusCode.OK, transitionResponse.StatusCode);
        }

        var reopenResponse = await client.PostAsJsonAsync(
            $"/api/solicitudes/{requestId}/reapertura",
            new
            {
                targetStatus = "InProgress",
                comment = "Need more investigation."
            });

        Assert.Equal(HttpStatusCode.OK, reopenResponse.StatusCode);
        var dto = await reopenResponse.Content.ReadFromJsonAsync<RequestDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(Domain.Enums.RequestStatus.InProgress, dto!.Status);
    }

    [Fact]
    public async Task ChangeStatus_ToClosedWithoutResolutionComment_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var requestId = await _factory.CreateRequestAsync(client);

        foreach (var transition in new[]
        {
            new { newStatus = "InReview", comment = "Triaged." },
            new { newStatus = "Assigned", comment = "Assigned." },
            new { newStatus = "InProgress", comment = "Working on it." },
            new { newStatus = "Resolved", comment = "Fixed." }
        })
        {
            await client.PatchAsJsonAsync(
                $"/api/solicitudes/{requestId}/estado",
                transition);
        }

        var response = await client.PatchAsJsonAsync(
            $"/api/solicitudes/{requestId}/estado",
            new { newStatus = "Closed", comment = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Contains("resolution", problem!.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListRequests_AsRequesterOnly_VisibleToRequester()
    {
        using var adminClient = await _factory.CreateAuthenticatedClientAsync();
        await _factory.CreateRequestAsync(adminClient);

        using var requesterClient = await _factory.CreateAuthenticatedClientAsync(
            username: "requester", password: "RequesterPass123!");
        var ownRequestId = await _factory.CreateRequestAsync(requesterClient);

        var requesterResponse = await requesterClient.GetAsync("/api/solicitudes?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, requesterResponse.StatusCode);

        var requesterPage = await ReadPageAsync(requesterResponse);
        Assert.Single(requesterPage.Items);
        Assert.Equal(ownRequestId, requesterPage.Items[0].Id);

        var adminResponse = await adminClient.GetAsync("/api/solicitudes?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        var adminPage = await ReadPageAsync(adminResponse);
        Assert.True(adminPage.TotalItems >= 2);
    }

    private static async Task<PaginatedResponse<RequestDto>> ReadPageAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<PaginatedResponse<RequestDto>>(JsonOptions))!;
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        private static readonly string InMemoryConnection = "Filename=:memory:";

        public const string RequesterUsername = "requester";
        public const string RequesterPassword = "RequesterPass123!";
        public const string AgentUsername = "agent";
        public const string AgentPassword = "AgentPass123!";
        public const string InactiveUsername = "inactive";
        public const string InactivePassword = "InactivePass123!";

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
            descriptor?.Let(d => services.Remove(d));
        }

        private static void OverrideJwtSettings(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(JwtSettings));
            descriptor?.Let(d => services.Remove(d));

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
                d => d.ServiceType == typeof(IRequestCodeGenerator));
            descriptor?.Let(d => services.Remove(d));

            services.AddSingleton<IRequestCodeGenerator, TestRequestCodeGenerator>();
        }

        private static void RegisterSqliteDatabase(IServiceCollection services)
        {
            var connection = new SqliteConnection(InMemoryConnection);
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
                    new User(RequesterUsername, "requester@example.local", hasher.Hash(RequesterPassword), UserRole.User),
                    new User(AgentUsername, "agent@example.local", hasher.Hash(AgentPassword), UserRole.User),
                    new User(InactiveUsername, "inactive@example.local", hasher.Hash(InactivePassword), UserRole.User, isActive: false));
                context.SaveChanges();
            }

            if (!context.Areas.Any())
            {
                context.Areas.AddRange(
                    new Area("Atención al Ciudadano"),
                    new Area("Soporte Técnico"),
                    new Area("Mantenimiento"),
                    new Area("Seguridad"),
                    new Area("Administración"),
                    new Area("Logística"));
                context.SaveChanges();
            }

            if (!context.RequestTypes.Any())
            {
                context.RequestTypes.AddRange(
                    new RequestType("Incidente", "Reporte de un incidente que requiere atención."),
                    new RequestType("Requerimiento", "Solicitud de un nuevo requerimiento o funcionalidad."),
                    new RequestType("Consulta", "Consulta general sobre servicios o procedimientos."),
                    new RequestType("Reclamo", "Reclamo formal sobre un servicio recibido."),
                    new RequestType("Mantenimiento Preventivo", "Solicitud programada de mantenimiento preventivo."),
                    new RequestType("Cambio", "Solicitud de cambio planificado en infraestructura o configuración."));
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

        public (Guid areaId, Guid requestTypeId) GetCatalogIds()
        {
            using var scope = Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var area = context.Areas.OrderBy(a => a.Name).First();
            var type = context.RequestTypes.OrderBy(t => t.Name).First();

            return (area.Id, type.Id);
        }

        public Guid GetActiveAgentId()
        {
            using var scope = Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return context.Users.Single(u => u.Username == AgentUsername).Id;
        }

        public Guid GetInactiveUserId()
        {
            using var scope = Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return context.Users.Single(u => u.Username == InactiveUsername).Id;
        }

        public async Task<Guid> CreateRequestAsync(HttpClient client)
        {
            var (areaId, requestTypeId) = GetCatalogIds();
            var title = "Test request " + Guid.NewGuid().ToString("N");

            var response = await client.PostAsJsonAsync("/api/solicitudes", new
            {
                title,
                description = "Test description",
                priority = "High",
                areaId,
                requestTypeId
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = (await response.Content.ReadFromJsonAsync<RequestDto>(JsonOptions))!;
            return dto.Id;
        }
    }
}

internal static class ServiceCollectionExtensions
{
    public static void Let<T>(this T? value, Action<T> action)
    {
        if (value is not null)
        {
            action(value);
        }
    }
}

internal sealed class TestRequestCodeGenerator : IRequestCodeGenerator
{
    private static long _counter;

    public Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        var next = Interlocked.Increment(ref _counter);
        var year = DateTime.UtcNow.Year;
        return Task.FromResult($"SOL-{year:D4}-{next:D4}");
    }
}
