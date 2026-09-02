using Application.Common.Results;
using Application.Institutions.Classifications;
using Application.Institutions.GetInstitutions;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Institutions.UpdateInstitution;

public sealed class UpdateInstitutionHandler
{
    private const int NameMaximumLength = Institution.NameMaximumLength;
    private const int ClassificationMaximumLength = 128;

    private static readonly Error NameRequiredError = Error.Validation(
        "Name is required.",
        "institutions.update.name.required");

    private static readonly Error NameTooLongError = Error.Validation(
        $"Name cannot exceed {NameMaximumLength} characters.",
        "institutions.update.name.too_long");

    private static readonly Error CategoryRequiredError = Error.Validation(
        "Category is required.",
        "institutions.update.category.required");

    private static readonly Error CategoryTooLongError = Error.Validation(
        $"Category cannot exceed {ClassificationMaximumLength} characters.",
        "institutions.update.category.too_long");

    private static readonly Error CategoryUnknownError = Error.Validation(
        "Category is not registered. Use one of the available classification options.",
        "institutions.update.category.unknown");

    private static readonly Error StatePowerRequiredError = Error.Validation(
        "StatePower is required.",
        "institutions.update.state_power.required");

    private static readonly Error StatePowerTooLongError = Error.Validation(
        $"StatePower cannot exceed {ClassificationMaximumLength} characters.",
        "institutions.update.state_power.too_long");

    private static readonly Error StatePowerUnknownError = Error.Validation(
        "StatePower is not registered. Use one of the available classification options.",
        "institutions.update.state_power.unknown");

    private static readonly Error SectorRequiredError = Error.Validation(
        "Sector is required.",
        "institutions.update.sector.required");

    private static readonly Error SectorTooLongError = Error.Validation(
        $"Sector cannot exceed {ClassificationMaximumLength} characters.",
        "institutions.update.sector.too_long");

    private static readonly Error SectorUnknownError = Error.Validation(
        "Sector is not registered. Use one of the available classification options.",
        "institutions.update.sector.unknown");

    private static readonly Error NotFoundError = Error.NotFound(
        "Institution not found.",
        "institutions.update.not_found");

    private static readonly Error DuplicateNameError = Error.Conflict(
        "An institution with the same name already exists.",
        "institutions.update.name.conflict");

    private readonly IInstitutionReadRepository _readRepository;
    private readonly IInstitutionWriteRepository _writeRepository;
    private readonly ICategoryReadRepository _categoryRepository;
    private readonly IStatePowerReadRepository _statePowerRepository;
    private readonly ISectorReadRepository _sectorRepository;
    private readonly ILogger<UpdateInstitutionHandler> _logger;

    public UpdateInstitutionHandler(
        IInstitutionReadRepository readRepository,
        IInstitutionWriteRepository writeRepository,
        ICategoryReadRepository categoryRepository,
        IStatePowerReadRepository statePowerRepository,
        ISectorReadRepository sectorRepository,
        ILogger<UpdateInstitutionHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _categoryRepository = categoryRepository;
        _statePowerRepository = statePowerRepository;
        _sectorRepository = sectorRepository;
        _logger = logger;
    }

    public async Task<Result<InstitutionDto>> HandleAsync(
        UpdateInstitutionCommand command,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation is not null)
        {
            _logger.LogWarning(
                "Rejected institution update. Kind=Validation Code={ErrorCode} Message={ErrorMessage}",
                validation.Code,
                validation.Message);
            return Result<InstitutionDto>.Failure(validation);
        }

        var institution = await _readRepository
            .FindByIdAsync(command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (institution is null)
        {
            _logger.LogWarning(
                "Institution update rejected because the institution was not found. InstitutionId={InstitutionId}",
                command.Id);
            return Result<InstitutionDto>.Failure(NotFoundError);
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
                "Rejected institution update because the category is not registered. InstitutionId={InstitutionId} Category={Category}",
                command.Id,
                categoryName);
            return Result<InstitutionDto>.Failure(CategoryUnknownError);
        }

        var statePower = await _statePowerRepository
            .FindByNameAsync(statePowerName, cancellationToken)
            .ConfigureAwait(false);
        if (statePower is null)
        {
            _logger.LogWarning(
                "Rejected institution update because the state power is not registered. InstitutionId={InstitutionId} StatePower={StatePower}",
                command.Id,
                statePowerName);
            return Result<InstitutionDto>.Failure(StatePowerUnknownError);
        }

        var sector = await _sectorRepository
            .FindByNameAsync(sectorName, cancellationToken)
            .ConfigureAwait(false);
        if (sector is null)
        {
            _logger.LogWarning(
                "Rejected institution update because the sector is not registered. InstitutionId={InstitutionId} Sector={Sector}",
                command.Id,
                sectorName);
            return Result<InstitutionDto>.Failure(SectorUnknownError);
        }

        institution.Update(name, category, statePower, sector);

        try
        {
            _writeRepository.Update(institution);
        }
        catch (DuplicateInstitutionNameException exception)
        {
            _logger.LogWarning(
                exception,
                "Institution update rejected because of duplicate name. InstitutionId={InstitutionId} Name={Name}",
                institution.Id,
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
                "Institution update failed. InstitutionId={InstitutionId}",
                institution.Id);
            return Result<InstitutionDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while updating the institution.",
                    "institutions.update.failed"));
        }

        _logger.LogInformation(
            "Institution updated. InstitutionId={InstitutionId} Name={Name}",
            institution.Id,
            institution.Name);

        return Result<InstitutionDto>.Success(new InstitutionDto(
            institution.Id,
            institution.Name,
            category.Name,
            statePower.Name,
            sector.Name));
    }

    private static Error? Validate(UpdateInstitutionCommand command)
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
