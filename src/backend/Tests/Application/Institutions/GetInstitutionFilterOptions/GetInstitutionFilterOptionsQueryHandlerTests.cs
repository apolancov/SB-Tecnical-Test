using Application.Common.Results;
using Application.Institutions.GetInstitutionFilterOptions;
using Application.Institutions.GetInstitutions.TestUtilities;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Institutions.GetInstitutionFilterOptions;

public class GetInstitutionFilterOptionsQueryHandlerTests
{
    private static readonly IReadOnlyList<Institution> SampleInstitutions = new[]
    {
        new Institution("Acuario Nacional", new Category("Organismo Descentralizado Funcionalmente"), new StatePower("Poder Ejecutivo"), new Sector("Medio Ambiente y Recursos Naturales")),
        new Institution("Archivo General de la Nación", new Category("Organismo Descentralizado Funcionalmente"), new StatePower("Poder Ejecutivo"), new Sector("Cultura")),
        new Institution("Ministerio de Cultura", new Category("Ministerio"), new StatePower("Poder Ejecutivo"), new Sector("Cultura")),
        new Institution("Universidad Autónoma de Santo Domingo", new Category("Universidad"), new StatePower("Poder Legislativo"), new Sector("Educación Superior")),
        new Institution("Senado de la República", new Category("Cámara Legislativa"), new StatePower("Poder Legislativo"), new Sector("Legislación"))
    };

    private static GetInstitutionFilterOptionsQueryHandler CreateHandler(
        InMemoryInstitutionReadRepository repository) =>
        new(repository, NullLogger<GetInstitutionFilterOptionsQueryHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ReturnsDistinctSortedCategories()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(new GetInstitutionFilterOptionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[] { "Cámara Legislativa", "Ministerio", "Organismo Descentralizado Funcionalmente", "Universidad" },
            result.Value.Categories);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDistinctSortedStatePowers()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(new GetInstitutionFilterOptionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Poder Ejecutivo", "Poder Legislativo" }, result.Value.StatePowers);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDistinctSortedSectors()
    {
        var repository = new InMemoryInstitutionReadRepository(SampleInstitutions);
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(new GetInstitutionFilterOptionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[] { "Cultura", "Educación Superior", "Legislación", "Medio Ambiente y Recursos Naturales" },
            result.Value.Sectors);
    }

    [Fact]
    public async Task HandleAsync_EmptyRepository_ReturnsEmptyLists()
    {
        var repository = new InMemoryInstitutionReadRepository(Array.Empty<Institution>());
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(new GetInstitutionFilterOptionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Categories);
        Assert.Empty(result.Value.StatePowers);
        Assert.Empty(result.Value.Sectors);
        Assert.Equal(1, repository.FilterOptionsCallCount);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_ReturnsUnexpectedFailure()
    {
        var repository = new ThrowingFilterOptionsRepository();
        var handler = new GetInstitutionFilterOptionsQueryHandler(
            repository,
            NullLogger<GetInstitutionFilterOptionsQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetInstitutionFilterOptionsQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Unexpected, result.Error!.Kind);
        Assert.Equal("institutions.filter_options.failed", result.Error.Code);
    }

    private sealed class ThrowingFilterOptionsRepository : IInstitutionFilterOptionsReadRepository
    {
        public Task<InstitutionFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated failure.");
        }
    }
}
