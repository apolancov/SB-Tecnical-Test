using Application.Common.Results;
using Application.Institutions.DeleteInstitution;
using Application.Institutions.GetInstitutions.TestUtilities;
using Application.Institutions.TestUtilities;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Institutions.DeleteInstitution;

public class DeleteInstitutionHandlerTests
{
    private static Institution Seed() =>
        new(
            "Original Name",
            new Category("Original Category"),
            new StatePower("Original Power"),
            new Sector("Original Sector"));

    [Fact]
    public async Task HandleAsync_WhenInstitutionExists_RemovesAndReturnsSuccess()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = new DeleteInstitutionHandler(
            readRepository,
            writeRepository,
            NullLogger<DeleteInstitutionHandler>.Instance);

        var result = await handler.HandleAsync(
            new DeleteInstitutionCommand(original.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, writeRepository.RemoveCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenInstitutionDoesNotExist_ReturnsNotFoundFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = new DeleteInstitutionHandler(
            readRepository,
            writeRepository,
            NullLogger<DeleteInstitutionHandler>.Instance);

        var result = await handler.HandleAsync(
            new DeleteInstitutionCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("institutions.delete.not_found", result.Error.Code);
        Assert.Equal(0, writeRepository.RemoveCallCount);
    }
}
