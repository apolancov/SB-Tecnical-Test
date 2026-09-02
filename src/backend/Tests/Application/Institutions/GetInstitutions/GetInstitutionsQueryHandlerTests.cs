using Application.Common.Results;
using Application.Institutions.GetInstitutions;
using Application.Institutions.GetInstitutions.TestUtilities;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Institutions.GetInstitutions;

public class GetInstitutionsQueryHandlerTests
{
    private static readonly IReadOnlyList<Institution> SampleInstitutions = new[]
    {
        new Institution("Acuario Nacional", new Category("Organismo Descentralizado Funcionalmente"), new StatePower("Poder Ejecutivo"), new Sector("Medio Ambiente y Recursos Naturales")),
        new Institution("Archivo General de la Nación", new Category("Organismo Descentralizado Funcionalmente"), new StatePower("Poder Ejecutivo"), new Sector("Cultura")),
        new Institution("Museo Nacional de Historia Natural", new Category("Organismo Descentralizado Funcionalmente"), new StatePower("Poder Ejecutivo"), new Sector("Cultura")),
        new Institution("Ministerio de Cultura", new Category("Ministerio"), new StatePower("Poder Ejecutivo"), new Sector("Cultura")),
        new Institution("Universidad Autónoma de Santo Domingo", new Category("Universidad"), new StatePower("Poder Ejecutivo"), new Sector("Educación Superior"))
    };

    private static GetInstitutionsQueryHandler CreateHandler(
        InMemoryInstitutionReadRepository repository) =>
        new(repository, NullLogger<GetInstitutionsQueryHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WithoutFilters_ReturnsAllInstitutionsOrderedByName()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalItems);
        Assert.Equal(5, result.Value.Items.Count);
        Assert.Equal(
            new[] { "Acuario Nacional", "Archivo General de la Nación", "Ministerio de Cultura", "Museo Nacional de Historia Natural", "Universidad Autónoma de Santo Domingo" },
            result.Value.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task HandleAsync_PaginatesResults()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 2, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalItems);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(new[] { "Acuario Nacional", "Archivo General de la Nación" }, result.Value.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task HandleAsync_ReturnsCorrectSecondPage()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(2, 2, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Ministerio de Cultura", "Museo Nacional de Historia Natural" }, result.Value.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task HandleAsync_FiltersByNamePartially()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, "Museo", null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("Museo Nacional de Historia Natural", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task HandleAsync_FiltersByCategory()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, null, "Ministerio", null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("Ministerio de Cultura", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task HandleAsync_FiltersByStatePower()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, null, null, "Poder Ejecutivo", null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalItems);
    }

    [Fact]
    public async Task HandleAsync_FiltersBySector()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, null, null, null, "Cultura");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Contains(result.Value.Items, item => item.Name == "Archivo General de la Nación");
        Assert.Contains(result.Value.Items, item => item.Name == "Museo Nacional de Historia Natural");
        Assert.Contains(result.Value.Items, item => item.Name == "Ministerio de Cultura");
    }

    [Fact]
    public async Task HandleAsync_CombinesFilters()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, null, null, "Poder Ejecutivo", "Cultura");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
    }

    [Fact]
    public async Task HandleAsync_TrimsWhitespaceFiltersBeforePassingToRepository()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, null, null, null, "   ");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalItems);
    }

    [Fact]
    public async Task HandleAsync_TreatsBlankFiltersAsAbsent()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, "", " ", null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalItems);
    }

    [Fact]
    public async Task HandleAsync_EmptyMatch_ReturnsEmptyPageWithMetadata()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, 20, "Nonexistent Institution", null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_PageBeyondLast_ReturnsEmptyItemsWithCorrectMetadata()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(10, 20, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(5, result.Value.TotalItems);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Equal(10, result.Value.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task HandleAsync_PageLessThanOne_ReturnsValidationFailure(int invalidPage)
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(invalidPage, 20, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public async Task HandleAsync_InvalidPageSize_ReturnsValidationFailure(int invalidPageSize)
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);
        var query = new GetInstitutionsQuery(1, invalidPageSize, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Fact]
    public async Task HandleAsync_BoundaryPageSize_IsAccepted()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);

        var minimum = await handler.HandleAsync(
            new GetInstitutionsQuery(1, 1, null, null, null, null), CancellationToken.None);
        var maximum = await handler.HandleAsync(
            new GetInstitutionsQuery(1, 100, null, null, null, null), CancellationToken.None);

        Assert.True(minimum.IsSuccess);
        Assert.True(maximum.IsSuccess);
    }
}