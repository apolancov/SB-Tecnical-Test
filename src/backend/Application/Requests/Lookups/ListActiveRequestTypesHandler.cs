using Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace Application.Requests.Lookups;

public sealed class ListActiveRequestTypesHandler
{
    private readonly IRequestTypeReadRepository _repository;
    private readonly ILogger<ListActiveRequestTypesHandler> _logger;

    public ListActiveRequestTypesHandler(
        IRequestTypeReadRepository repository,
        ILogger<ListActiveRequestTypesHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<RequestTypeLookupDto>>> HandleAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var types = await _repository
                .ListActiveAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Active request types listed. Count={Count}",
                types.Count);

            IReadOnlyList<RequestTypeLookupDto> items = types
                .Select(type => new RequestTypeLookupDto(type.Id, type.Name, type.Description))
                .ToList();

            return Result<IReadOnlyList<RequestTypeLookupDto>>.Success(items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Active request types lookup failed.");
            return Result<IReadOnlyList<RequestTypeLookupDto>>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while reading request types.",
                    "requests.lookups.request_types.failed"));
        }
    }
}