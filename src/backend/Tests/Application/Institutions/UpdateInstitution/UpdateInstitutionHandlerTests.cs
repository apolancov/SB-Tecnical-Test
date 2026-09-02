using Application.Common.Results;
using Application.Institutions;
using Application.Institutions.Classifications;
using Application.Institutions.GetInstitutions.TestUtilities;
using Application.Institutions.TestUtilities;
using Application.Institutions.UpdateInstitution;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Institutions.UpdateInstitution;

public class UpdateInstitutionHandlerTests
{
    private const string DefaultCategoryName = "Original Category";
    private const string DefaultStatePowerName = "Original Power";
    private const string DefaultSectorName = "Original Sector";

    private static Institution Seed() =>
        new(
            "Original Name",
            new Category(DefaultCategoryName),
            new StatePower(DefaultStatePowerName),
            new Sector(DefaultSectorName));

    private static UpdateInstitutionHandler CreateHandler(
        InMemoryInstitutionReadRepository readRepository,
        InMemoryInstitutionWriteRepository writeRepository,
        ICategoryReadRepository? categoryRepository = null,
        IStatePowerReadRepository? statePowerRepository = null,
        ISectorReadRepository? sectorRepository = null) =>
        new(
            readRepository,
            writeRepository,
            categoryRepository ?? new InMemoryCategoryReadRepository(DefaultCategoryName, "Updated Category"),
            statePowerRepository ?? new InMemoryStatePowerReadRepository(DefaultStatePowerName, "Updated Power"),
            sectorRepository ?? new InMemorySectorReadRepository(DefaultSectorName, "Updated Sector"),
            NullLogger<UpdateInstitutionHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WithValidArguments_UpdatesAndReturnsDto()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: original.Id,
            Name: "Updated Name",
            Category: "Updated Category",
            StatePower: "Updated Power",
            Sector: "Updated Sector");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(original.Id, result.Value.Id);
        Assert.Equal("Updated Name", result.Value.Name);
        Assert.Equal("Updated Category", result.Value.Category);
        Assert.Equal("Updated Power", result.Value.StatePower);
        Assert.Equal("Updated Sector", result.Value.Sector);
        Assert.Equal(1, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenInstitutionNotFound_ReturnsNotFoundFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Name",
            Category: "Updated Category",
            StatePower: "Updated Power",
            Sector: "Updated Sector");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("institutions.update.not_found", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyName_ReturnsValidationFailure(string? invalidName)
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: original.Id,
            Name: invalidName!,
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.update.name.required", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithNameTooLong_ReturnsValidationFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: original.Id,
            Name: new string('a', 257),
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.update.name.too_long", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownCategory_ReturnsValidationFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: original.Id,
            Name: "Updated Name",
            Category: "Unregistered Category",
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.update.category.unknown", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownStatePower_ReturnsValidationFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: original.Id,
            Name: "Updated Name",
            Category: DefaultCategoryName,
            StatePower: "Unregistered Power",
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.update.state_power.unknown", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownSector_ReturnsValidationFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: original.Id,
            Name: "Updated Name",
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: "Unregistered Sector");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.update.sector.unknown", result.Error.Code);
        Assert.Equal(0, writeRepository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateName_ReturnsConflictFailure()
    {
        var original = Seed();
        var readRepository = new InMemoryInstitutionReadRepository(new[] { original });
        var writeRepository = new InMemoryInstitutionWriteRepository(
            onUpdate: institution =>
            {
                throw new DuplicateInstitutionNameException(institution.Name);
            });
        var handler = CreateHandler(readRepository, writeRepository);

        var command = new UpdateInstitutionCommand(
            Id: original.Id,
            Name: "Conflicting Name",
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Equal("institutions.update.name.conflict", result.Error.Code);
    }

    private sealed class InMemoryCategoryReadRepository : ICategoryReadRepository
    {
        private readonly HashSet<string> _knownNames;

        public InMemoryCategoryReadRepository(params string[] knownNames)
        {
            _knownNames = new HashSet<string>(knownNames, StringComparer.Ordinal);
        }

        public Task<Category?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(normalizedName) || !_knownNames.Contains(normalizedName))
            {
                return Task.FromResult<Category?>(null);
            }

            return Task.FromResult<Category?>(new Category(normalizedName));
        }
    }

    private sealed class InMemoryStatePowerReadRepository : IStatePowerReadRepository
    {
        private readonly HashSet<string> _knownNames;

        public InMemoryStatePowerReadRepository(params string[] knownNames)
        {
            _knownNames = new HashSet<string>(knownNames, StringComparer.Ordinal);
        }

        public Task<StatePower?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(normalizedName) || !_knownNames.Contains(normalizedName))
            {
                return Task.FromResult<StatePower?>(null);
            }

            return Task.FromResult<StatePower?>(new StatePower(normalizedName));
        }
    }

    private sealed class InMemorySectorReadRepository : ISectorReadRepository
    {
        private readonly HashSet<string> _knownNames;

        public InMemorySectorReadRepository(params string[] knownNames)
        {
            _knownNames = new HashSet<string>(knownNames, StringComparer.Ordinal);
        }

        public Task<Sector?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(normalizedName) || !_knownNames.Contains(normalizedName))
            {
                return Task.FromResult<Sector?>(null);
            }

            return Task.FromResult<Sector?>(new Sector(normalizedName));
        }
    }
}