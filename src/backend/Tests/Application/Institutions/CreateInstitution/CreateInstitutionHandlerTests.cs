using Application.Common.Results;
using Application.Institutions;
using Application.Institutions.Classifications;
using Application.Institutions.CreateInstitution;
using Application.Institutions.TestUtilities;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Institutions.CreateInstitution;

public class CreateInstitutionHandlerTests
{
    private const string DefaultCategoryName = "Ministerio";
    private const string DefaultStatePowerName = "Poder Ejecutivo";
    private const string DefaultSectorName = "Hacienda";

    private static CreateInstitutionCommand ValidCommand(
        string name = "New Institution",
        string category = DefaultCategoryName,
        string statePower = DefaultStatePowerName,
        string sector = DefaultSectorName) =>
        new(name, category, statePower, sector);

    private static CreateInstitutionHandler CreateHandler(
        IInstitutionWriteRepository writeRepository,
        ICategoryReadRepository? categoryRepository = null,
        IStatePowerReadRepository? statePowerRepository = null,
        ISectorReadRepository? sectorRepository = null) =>
        new(
            writeRepository,
            categoryRepository ?? new InMemoryCategoryReadRepository(DefaultCategoryName),
            statePowerRepository ?? new InMemoryStatePowerReadRepository(DefaultStatePowerName),
            sectorRepository ?? new InMemorySectorReadRepository(DefaultSectorName),
            NullLogger<CreateInstitutionHandler>.Instance);

    [Fact]
    public async Task HandleAsync_WithValidArguments_PersistsInstitutionAndReturnsDto()
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("New Institution", result.Value.Name);
        Assert.Equal(DefaultCategoryName, result.Value.Category);
        Assert.Equal(DefaultStatePowerName, result.Value.StatePower);
        Assert.Equal(DefaultSectorName, result.Value.Sector);
        Assert.Equal(1, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_TrimsWhitespaceFromArguments()
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "  Padded Name  ",
            Category: $" {DefaultCategoryName} ",
            StatePower: $" {DefaultStatePowerName} ",
            Sector: $" {DefaultSectorName} ");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Padded Name", result.Value.Name);
        Assert.Equal(DefaultCategoryName, result.Value.Category);
        Assert.Equal(DefaultStatePowerName, result.Value.StatePower);
        Assert.Equal(DefaultSectorName, result.Value.Sector);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyName_ReturnsValidationFailure(string? invalidName)
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: invalidName!,
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.name.required", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyCategory_ReturnsValidationFailure(string? invalidCategory)
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "Name",
            Category: invalidCategory!,
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.category.required", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptyStatePower_ReturnsValidationFailure(string? invalidStatePower)
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "Name",
            Category: DefaultCategoryName,
            StatePower: invalidStatePower!,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.state_power.required", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithNullOrEmptySector_ReturnsValidationFailure(string? invalidSector)
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "Name",
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: invalidSector!);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.sector.required", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithNameTooLong_ReturnsValidationFailure()
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: new string('a', 257),
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.name.too_long", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownCategory_ReturnsValidationFailure()
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "Name",
            Category: "Unregistered Category",
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.category.unknown", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownStatePower_ReturnsValidationFailure()
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "Name",
            Category: DefaultCategoryName,
            StatePower: "Unregistered Power",
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.state_power.unknown", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownSector_ReturnsValidationFailure()
    {
        var writeRepository = new InMemoryInstitutionWriteRepository();
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "Name",
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: "Unregistered Sector");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Equal("institutions.create.sector.unknown", result.Error.Code);
        Assert.Equal(0, writeRepository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateName_ReturnsConflictFailure()
    {
        var writeRepository = new InMemoryInstitutionWriteRepository(
            onAdd: institution =>
            {
                if (institution.Name == "Duplicate Name")
                {
                    throw new DuplicateInstitutionNameException(institution.Name);
                }
            });
        var handler = CreateHandler(writeRepository);

        var command = new CreateInstitutionCommand(
            Name: "Duplicate Name",
            Category: DefaultCategoryName,
            StatePower: DefaultStatePowerName,
            Sector: DefaultSectorName);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Equal("institutions.create.name.conflict", result.Error.Code);
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