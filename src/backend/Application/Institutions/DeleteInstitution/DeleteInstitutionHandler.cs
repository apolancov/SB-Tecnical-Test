using Application.Common.Results;
using Application.Institutions.GetInstitutions;
using Microsoft.Extensions.Logging;

namespace Application.Institutions.DeleteInstitution;

public sealed class DeleteInstitutionHandler
{
    private static readonly Error NotFoundError = Error.NotFound(
        "Institution not found.",
        "institutions.delete.not_found");

    private readonly IInstitutionReadRepository _readRepository;
    private readonly IInstitutionWriteRepository _writeRepository;
    private readonly ILogger<DeleteInstitutionHandler> _logger;

    public DeleteInstitutionHandler(
        IInstitutionReadRepository readRepository,
        IInstitutionWriteRepository writeRepository,
        ILogger<DeleteInstitutionHandler> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        DeleteInstitutionCommand command,
        CancellationToken cancellationToken)
    {
        var institution = await _readRepository
            .FindByIdAsync(command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (institution is null)
        {
            _logger.LogWarning(
                "Institution deletion rejected because the institution was not found. InstitutionId={InstitutionId}",
                command.Id);
            return Result.Failure(NotFoundError);
        }

        try
        {
            _writeRepository.Remove(institution);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Institution deletion failed. InstitutionId={InstitutionId}",
                institution.Id);
            return Result.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while deleting the institution.",
                    "institutions.delete.failed"));
        }

        _logger.LogInformation(
            "Institution deleted. InstitutionId={InstitutionId}",
            institution.Id);

        return Result.Success();
    }
}
