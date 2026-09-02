using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Requests.Lookups;

public sealed class ListActiveAreasHandler
{
    private readonly IAreaReadRepository _repository;
    private readonly ILogger<ListActiveAreasHandler> _logger;

    public ListActiveAreasHandler(
        IAreaReadRepository repository,
        ILogger<ListActiveAreasHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AreaLookupDto>>> HandleAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var areas = await _repository
                .ListActiveAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Active areas listed. Count={Count}",
                areas.Count);

            IReadOnlyList<AreaLookupDto> items = areas
                .Select(area => new AreaLookupDto(area.Id, area.Name))
                .ToList();

            return Result<IReadOnlyList<AreaLookupDto>>.Success(items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Active areas lookup failed.");
            return Result<IReadOnlyList<AreaLookupDto>>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while reading areas.",
                    "requests.lookups.areas.failed"));
        }
    }
}