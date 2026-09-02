using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Functional;
using Application.Authentication.AuthenticateUser;
using Application.Common.Pagination;
using Application.Institutions.GetInstitutions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Api.Functional;

public class InstitutionsCrudApiTests : IClassFixture<InstitutionsApiTests.Factory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly InstitutionsApiTests.Factory _factory;

    public InstitutionsCrudApiTests(InstitutionsApiTests.Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateInstitution_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/institutions", new
        {
            name = "Sample",
            category = "Ministerio",
            statePower = "Poder Ejecutivo",
            sector = "Hacienda",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateInstitution_WithValidPayload_ReturnsCreatedAndLocationHeader()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var uniqueName = "Created Institution " + Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync("/api/institutions", new
        {
            name = uniqueName,
            category = "Ministerio",
            statePower = "Poder Ejecutivo",
            sector = "Hacienda",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var dto = await response.Content.ReadFromJsonAsync<InstitutionDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(uniqueName, dto!.Name);
        Assert.Equal("Ministerio", dto.Category);
        Assert.Equal("Poder Ejecutivo", dto.StatePower);
        Assert.Equal("Hacienda", dto.Sector);
        Assert.NotEqual(Guid.Empty, dto.Id);
    }

    [Fact]
    public async Task CreateInstitution_WithMissingName_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/institutions", new
        {
            name = "   ",
            category = "Ministerio",
            statePower = "Poder Ejecutivo",
            sector = "Hacienda",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("institutions.create.name.required", problem!.Type);
    }

    [Fact]
    public async Task CreateInstitution_WithDuplicateName_ReturnsConflict()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/institutions", new
        {
            name = "Acuario Nacional",
            category = "Ministerio",
            statePower = "Poder Ejecutivo",
            sector = "Hacienda",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("institutions.create.name.conflict", problem!.Type);
    }

    [Fact]
    public async Task GetInstitutionById_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/institutions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInstitutionById_WithUnknownId_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/institutions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("institutions.read.not_found", problem!.Type);
    }

    [Fact]
    public async Task GetInstitutionById_WithExistingId_ReturnsInstitution()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var list = await client.GetFromJsonAsync<PaginatedResponse<InstitutionDto>>(
            "/api/institutions?page=1&pageSize=1",
            JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list!.Items);

        var existing = list.Items[0];

        var response = await client.GetAsync($"/api/institutions/{existing.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<InstitutionDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(existing.Id, dto!.Id);
        Assert.Equal(existing.Name, dto.Name);
        Assert.Equal(existing.Category, dto.Category);
        Assert.Equal(existing.StatePower, dto.StatePower);
        Assert.Equal(existing.Sector, dto.Sector);
    }

    [Fact]
    public async Task UpdateInstitution_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/institutions/{Guid.NewGuid()}", new
        {
            name = "Updated",
            category = "Cat",
            statePower = "Power",
            sector = "Sector",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateInstitution_WithUnknownId_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/institutions/{Guid.NewGuid()}", new
        {
            name = "Updated",
            category = "Cat",
            statePower = "Power",
            sector = "Sector",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateInstitution_WithValidPayload_UpdatesAndReturnsUpdatedInstitution()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var uniqueName = "Update Me " + Guid.NewGuid().ToString("N");
        var createResponse = await client.PostAsJsonAsync("/api/institutions", new
        {
            name = uniqueName,
            category = "Ministerio",
            statePower = "Poder Ejecutivo",
            sector = "Hacienda",
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<InstitutionDto>(JsonOptions))!;

        var newName = "Renamed " + Guid.NewGuid().ToString("N");
        var updateResponse = await client.PutAsJsonAsync($"/api/institutions/{created.Id}", new
        {
            name = newName,
            category = "Organismo Descentralizado Funcionalmente",
            statePower = "Poder Ejecutivo",
            sector = "Cultura",
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = (await updateResponse.Content.ReadFromJsonAsync<InstitutionDto>(JsonOptions))!;
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(newName, updated.Name);
        Assert.Equal("Organismo Descentralizado Funcionalmente", updated.Category);
        Assert.Equal("Cultura", updated.Sector);

        var readResponse = await client.GetAsync($"/api/institutions/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var readBack = (await readResponse.Content.ReadFromJsonAsync<InstitutionDto>(JsonOptions))!;
        Assert.Equal(newName, readBack.Name);
    }

    [Fact]
    public async Task UpdateInstitution_WithDuplicateName_ReturnsConflict()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var list = await client.GetFromJsonAsync<PaginatedResponse<InstitutionDto>>(
            "/api/institutions?page=1&pageSize=2",
            JsonOptions);
        Assert.NotNull(list);
        Assert.True(list!.Items.Count >= 2);

        var first = list.Items[0];
        var second = list.Items[1];

        var response = await client.PutAsJsonAsync($"/api/institutions/{second.Id}", new
        {
            name = first.Name,
            category = second.Category,
            statePower = second.StatePower,
            sector = second.Sector,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInstitution_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/institutions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInstitution_WithUnknownId_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/institutions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInstitution_WithExistingId_RemovesAndReturnsNoContent()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var uniqueName = "Delete Me " + Guid.NewGuid().ToString("N");
        var createResponse = await client.PostAsJsonAsync("/api/institutions", new
        {
            name = uniqueName,
            category = "Ministerio",
            statePower = "Poder Ejecutivo",
            sector = "Hacienda",
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<InstitutionDto>(JsonOptions))!;

        var deleteResponse = await client.DeleteAsync($"/api/institutions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var readResponse = await client.GetAsync($"/api/institutions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, readResponse.StatusCode);
    }
}
