using Application.Common.Results;
using Application.Institutions.Classifications;
using Application.Institutions.GetInstitutions;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Institutions.CreateInstitution;

public sealed class CreateInstitutionHandler
{
    private const int NameMaximumLength = Institution.NameMaximumLength;
    private const int ClassificationMaximumLength = 128;

    private static readonly Error NameRequiredError = Error.Validation(
        "Name is required.",
        "institutions.create.name.required");

    private static readonly Error NameTooLongError = Error.Validation(
        $"Name cannot exceed {NameMaximumLength} characters.",
        "institutions.create.name.too_long");

    private static readonly Error CategoryRequiredError = Error.Validation(
        "Category is required.",
        "institutions.create.category.required");

    private static readonly Error CategoryTooLongError = Error.Validation(
        $"Category cannot exceed {ClassificationMaximumLength} characters.",
        "institutions.create.category.too_long");

    private static readonly Error CategoryUnknownError = Error.Validation(
        "Category is not registered. Use one of the available classification options.",
        "institutions.create.category.unknown");

    private static readonly Error StatePowerRequiredError = Error.Validation(
        "StatePower is required.",
        "institutions.create.state_power.required");

    private static readonly Error StatePowerTooLongError = Error.Validation(
        $"StatePower cannot exceed {ClassificationMaximumLength} characters.",
        "institutions.create.state_power.too_long");

    private static readonly Error StatePowerUnknownError = Error.Validation(
        "StatePower is not registered. Use one of the available classification options.",
        "institutions.create.state_power.unknown");

    private static readonly Error SectorRequiredError = Error.Validation(
        "Sector is required.",
        "institutions.create.sector.required");

    private static readonly Error SectorTooLongError = Error.Validation(
        $"Sector cannot exceed {ClassificationMaximumLength} characters.",
        "institutions.create.sector.too_long");

    private static readonly Error SectorUnknownError = Error.Validation(
        "Sector is not registered. Use one of the available classification options.",
        "institutions.create.sector.unknown");

    private static readonly Error DuplicateNameError = Error.Conflict(
        "An institution with the same name already exists.",
        "institutions.create.name.conflict");

    private readonly IInstitutionWriteRepository _repository;
    private readonly ICategoryReadRepository _categoryRepository;
    private readonly IStatePowerReadRepository _statePowerRepository;
    private readonly ISectorReadRepository _sectorRepository;
    private readonly ILogger<CreateInstitutionHandler> _logger;

    public CreateInstitutionHandler(
        IInstitutionWriteRepository repository,
        ICategoryReadRepository categoryRepository,
        IStatePowerReadRepository statePowerRepository,
        ISectorReadRepository sectorRepository,
        ILogger<CreateInstitutionHandler> logger)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _statePowerRepository = statePowerRepository;
        _sectorRepository = sectorRepository;
        _logger = logger;
    }

    public async Task<Result<InstitutionDto>> HandleAsync(
        CreateInstitutionCommand command,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation is not null)
        {
            _logger.LogWarning(
                "Rejected institution creation. Kind=Validation Code={ErrorCode} Message={ErrorMessage}",
                validation.Code,
                validation.Message);
            return Result<InstitutionDto>.Failure(validation);
        }

        var name = command.Name.Trim();
        var categoryName = command.Category.Trim();
        var statePowerName = command.StatePower.Trim();
        var sectorName = command.Sector.Trim();

        var category = await _categoryRepository
            .FindByNameAsync(categoryName, cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            _logger.LogWarning(
                "Rejected institution creation because the category is not registered. Category={Category}",
                categoryName);
            return Result<InstitutionDto>.Failure(CategoryUnknownError);
        }

        var statePower = await _statePowerRepository
            .FindByNameAsync(statePowerName, cancellationToken)
            .ConfigureAwait(false);
        if (statePower is null)
        {
            _logger.LogWarning(
                "Rejected institution creation because the state power is not registered. StatePower={StatePower}",
                statePowerName);
            return Result<InstitutionDto>.Failure(StatePowerUnknownError);
        }

        var sector = await _sectorRepository
            .FindByNameAsync(sectorName, cancellationToken)
            .ConfigureAwait(false);
        if (sector is null)
        {
            _logger.LogWarning(
                "Rejected institution creation because the sector is not registered. Sector={Sector}",
                sectorName);
            return Result<InstitutionDto>.Failure(SectorUnknownError);
        }

        var institution = new Institution(name, category, statePower, sector);

        try
        {
            await _repository.AddAsync(institution, cancellationToken);
        }
        catch (DuplicateInstitutionNameException exception)
        {
            _logger.LogWarning(
                exception,
                "Institution creation rejected because of duplicate name. Name={Name}",
                institution.Name);
            return Result<InstitutionDto>.Failure(DuplicateNameError);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Institution creation failed. Name={Name}",
                institution.Name);
            return Result<InstitutionDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while creating the institution.",
                    "institutions.create.failed"));
        }

        _logger.LogInformation(
            "Institution created. InstitutionId={InstitutionId} Name={Name}",
            institution.Id,
            institution.Name);

        return Result<InstitutionDto>.Success(new InstitutionDto(
            institution.Id,
            institution.Name,
            category.Name,
            statePower.Name,
            sector.Name));
    }

    private static Error? Validate(CreateInstitutionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return NameRequiredError;
        }

        var name = command.Name.Trim();
        if (name.Length > NameMaximumLength)
        {
            return NameTooLongError;
        }

        if (string.IsNullOrWhiteSpace(command.Category))
        {
            return CategoryRequiredError;
        }

        var category = command.Category.Trim();
        if (category.Length > ClassificationMaximumLength)
        {
            return CategoryTooLongError;
        }

        if (string.IsNullOrWhiteSpace(command.StatePower))
        {
            return StatePowerRequiredError;
        }

        var statePower = command.StatePower.Trim();
        if (statePower.Length > ClassificationMaximumLength)
        {
            return StatePowerTooLongError;
        }

        if (string.IsNullOrWhiteSpace(command.Sector))
        {
            return SectorRequiredError;
        }

        var sector = command.Sector.Trim();
        if (sector.Length > ClassificationMaximumLength)
        {
            return SectorTooLongError;
        }

        return null;
    }
}
