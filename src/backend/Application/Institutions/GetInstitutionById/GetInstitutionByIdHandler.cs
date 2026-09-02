using Application.Common.Results;
using Application.Institutions.GetInstitutions;
using Microsoft.Extensions.Logging;

namespace Application.Institutions.GetInstitutionById;

public sealed class GetInstitutionByIdHandler
{
    private static readonly Error NotFoundError = Error.NotFound(
        "Institution not found.",
        "institutions.read.not_found");

    private readonly IInstitutionReadRepository _repository;
    private readonly ILogger<GetInstitutionByIdHandler> _logger;

    public GetInstitutionByIdHandler(
        IInstitutionReadRepository repository,
        ILogger<GetInstitutionByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<InstitutionDto>> HandleAsync(
        GetInstitutionByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Id == Guid.Empty)
        {
            _logger.LogWarning(
                "Rejected institution read because the identifier was empty.");
            return Result<InstitutionDto>.Failure(NotFoundError);
        }

        var institution = await _repository
            .FindByIdAsync(query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (institution is null)
        {
            _logger.LogInformation(
                "Institution read returned no result. InstitutionId={InstitutionId}",
                query.Id);
            return Result<InstitutionDto>.Failure(NotFoundError);
        }

        return Result<InstitutionDto>.Success(new InstitutionDto(
            institution.Id,
            institution.Name,
            institution.Category.Name,
            institution.StatePower.Name,
            institution.Sector.Name));
    }
}
