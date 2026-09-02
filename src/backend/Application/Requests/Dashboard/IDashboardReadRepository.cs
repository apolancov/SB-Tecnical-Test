using Domain.Enums;

namespace Application.Requests.Dashboard;

public interface IDashboardReadRepository
{
    Task<int> CountRequestsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<RequestPriority, int>> CountByPriorityAsync(
        CancellationToken cancellationToken);

    Task<int> CountPendingAsync(CancellationToken cancellationToken);

    Task<int> CountAssignedAsync(CancellationToken cancellationToken);

    Task<int> CountUnassignedAsync(CancellationToken cancellationToken);

    Task<int> CountByStatusAsync(RequestStatus status, CancellationToken cancellationToken);

    Task<int> CountByPriorityAsync(RequestPriority priority, CancellationToken cancellationToken);

    Task<int> CountOverdueAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestSummaryDto>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken);
}