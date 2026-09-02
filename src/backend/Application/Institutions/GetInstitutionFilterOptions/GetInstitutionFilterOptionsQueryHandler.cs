using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Institutions.GetInstitutionFilterOptions;

public sealed class GetInstitutionFilterOptionsQueryHandler
{
    private readonly IInstitutionFilterOptionsReadRepository _repository;
    private readonly ILogger<GetInstitutionFilterOptionsQueryHandler> _logger;

    public GetInstitutionFilterOptionsQueryHandler(
        IInstitutionFilterOptionsReadRepository repository,
        ILogger<GetInstitutionFilterOptionsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<InstitutionFilterOptionsDto>> HandleAsync(
        GetInstitutionFilterOptionsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = await _repository.GetFilterOptionsAsync(cancellationToken);

            _logger.LogInformation(
                "Institution filter options loaded. "
                    + "Categories={CategoryCount} StatePowers={StatePowerCount} Sectors={SectorCount}",
                options.Categories.Count,
                options.StatePowers.Count,
                options.Sectors.Count);

            return Result<InstitutionFilterOptionsDto>.Success(options);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Institution filter options query failed.");

            return Result<InstitutionFilterOptionsDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while loading institution filter options.",
                    "institutions.filter_options.failed"));
        }
    }
}
