using Application.Common.Results;
using Application.Institutions.GetInstitutionById;
using Application.Institutions.GetInstitutions.TestUtilities;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Institutions.GetInstitutionById;

public class GetInstitutionByIdHandlerTests
{
    private static Institution Seed() =>
        new(
            "Acuario Nacional",
            new Category("Categoria"),
            new StatePower("Poder Ejecutivo"),
            new Sector("Sector"));

    [Fact]
    public async Task HandleAsync_WhenInstitutionExists_ReturnsDto()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var handler = new GetInstitutionByIdHandler(
            readRepository,
            NullLogger<GetInstitutionByIdHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetInstitutionByIdQuery(original.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(original.Id, result.Value.Id);
        Assert.Equal("Acuario Nacional", result.Value.Name);
        Assert.Equal(1, readRepository.FindByIdCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenInstitutionDoesNotExist_ReturnsNotFoundFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var handler = new GetInstitutionByIdHandler(
            readRepository,
            NullLogger<GetInstitutionByIdHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetInstitutionByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("institutions.read.not_found", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyGuid_ReturnsNotFoundFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var handler = new GetInstitutionByIdHandler(
            readRepository,
            NullLogger<GetInstitutionByIdHandler>.Instance);

        var result = await handler.HandleAsync(
            new GetInstitutionByIdQuery(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("institutions.read.not_found", result.Error.Code);
        Assert.Equal(0, readRepository.FindByIdCallCount);
    }
}