using Application.Requests;
using Application.Requests.Dashboard;
using Domain.Enums;

namespace Application.Requests.TestUtilities;

public sealed class InMemoryDashboardReadRepository : IDashboardReadRepository
{
    private readonly List<Domain.Entities.Request> _requests;

    public InMemoryDashboardReadRepository(IEnumerable<Domain.Entities.Request>? requests = null)
    {
        _requests = requests?.ToList() ?? new List<Domain.Entities.Request>();
    }

    private static readonly RequestStatus[] PendingStatuses =
    {
        RequestStatus.Submitted,
        RequestStatus.InReview,
        RequestStatus.Assigned,
        RequestStatus.InProgress,
        RequestStatus.OnHold
    };

    public Task<int> CountRequestsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_requests.Count);
    }

    public Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken)
    {
        var dictionary = new Dictionary<RequestStatus, int>();
        foreach (RequestStatus status in Enum.GetValues<RequestStatus>())
        {
            dictionary[status] = 0;
        }

        foreach (var request in _requests)
        {
            dictionary[request.Status] = dictionary.GetValueOrDefault(request.Status) + 1;
        }

        return Task.FromResult<IReadOnlyDictionary<RequestStatus, int>>(dictionary);
    }

    public Task<IReadOnlyDictionary<RequestPriority, int>> CountByPriorityAsync(
        CancellationToken cancellationToken)
    {
        var dictionary = new Dictionary<RequestPriority, int>();
        foreach (RequestPriority priority in Enum.GetValues<RequestPriority>())
        {
            dictionary[priority] = 0;
        }

        foreach (var request in _requests)
        {
            dictionary[request.Priority] = dictionary.GetValueOrDefault(request.Priority) + 1;
        }

        return Task.FromResult<IReadOnlyDictionary<RequestPriority, int>>(dictionary);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_requests.Count(r => PendingStatuses.Contains(r.Status)));
    }

    public Task<int> CountAssignedAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_requests.Count(r => r.ResponsibleId != null));
    }

    public Task<int> CountUnassignedAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_requests.Count(r => r.ResponsibleId == null));
    }

    public Task<int> CountByStatusAsync(RequestStatus status, CancellationToken cancellationToken)
    {
        return Task.FromResult(_requests.Count(r => r.Status == status));
    }

    public Task<int> CountByPriorityAsync(RequestPriority priority, CancellationToken cancellationToken)
    {
        return Task.FromResult(_requests.Count(r => r.Priority == priority));
    }

    public Task<int> CountOverdueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return Task.FromResult(_requests.Count(r =>
            r.DueDate.HasValue
            && r.DueDate.Value < now
            && r.ClosedAt is null
            && PendingStatuses.Contains(r.Status)));
    }

    public Task<IReadOnlyList<RequestSummaryDto>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return Task.FromResult<IReadOnlyList<RequestSummaryDto>>(Array.Empty<RequestSummaryDto>());
        }

        IReadOnlyList<RequestSummaryDto> recent = _requests
            .OrderByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Take(count)
            .Select(r => new RequestSummaryDto(
                r.Id,
                r.Code,
                r.Title,
                r.Priority,
                r.Status,
                r.CreatedAt,
                r.DueDate,
                r.RequesterId,
                r.Requester.Username,
                r.ResponsibleId,
                r.Responsible?.Username))
            .ToList();

        return Task.FromResult(recent);
    }
}