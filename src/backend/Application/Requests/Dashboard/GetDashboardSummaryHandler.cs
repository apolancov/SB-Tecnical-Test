using Application.Common.Results;
using Application.Requests;
using Microsoft.Extensions.Logging;

namespace Application.Requests.Dashboard;

public sealed class GetDashboardSummaryHandler
{
    private const int RecentRequestsCount = 5;

    private readonly IDashboardReadRepository _repository;
    private readonly ILogger<GetDashboardSummaryHandler> _logger;

    public GetDashboardSummaryHandler(
        IDashboardReadRepository repository,
        ILogger<GetDashboardSummaryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<DashboardSummaryDto>> HandleAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var totalRequests = await _repository
                .CountRequestsAsync(cancellationToken)
                .ConfigureAwait(false);

            var requestsByStatus = await _repository
                .CountByStatusAsync(cancellationToken)
                .ConfigureAwait(false);

            var requestsByPriority = await _repository
                .CountByPriorityAsync(cancellationToken)
                .ConfigureAwait(false);

            var pendingRequests = await _repository
                .CountPendingAsync(cancellationToken)
                .ConfigureAwait(false);

            var assignedRequests = await _repository
                .CountAssignedAsync(cancellationToken)
                .ConfigureAwait(false);

            var unassignedRequests = await _repository
                .CountUnassignedAsync(cancellationToken)
                .ConfigureAwait(false);

            var overdueRequests = await _repository
                .CountOverdueAsync(cancellationToken)
                .ConfigureAwait(false);

            var recent = await _repository
                .GetRecentAsync(RecentRequestsCount, cancellationToken)
                .ConfigureAwait(false);

            var summary = new DashboardSummaryDto(
                TotalRequests: totalRequests,
                RequestsByStatus: requestsByStatus,
                RequestsByPriority: requestsByPriority,
                OverdueRequests: overdueRequests,
                PendingRequests: pendingRequests,
                AssignedRequests: assignedRequests,
                UnassignedRequests: unassignedRequests,
                RecentRequests: recent,
                GeneratedAtUtc: DateTime.UtcNow);

            _logger.LogInformation(
                "Dashboard summary computed. TotalRequests={TotalRequests} Pending={Pending} Assigned={Assigned} Unassigned={Unassigned} Overdue={Overdue}",
                summary.TotalRequests,
                summary.PendingRequests,
                summary.AssignedRequests,
                summary.UnassignedRequests,
                summary.OverdueRequests);

            return Result<DashboardSummaryDto>.Success(summary);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dashboard summary generation failed.");
            return Result<DashboardSummaryDto>.Failure(
                Error.Unexpected(
                    "An unexpected error occurred while computing the dashboard summary.",
                    "requests.dashboard.summary.failed"));
        }
    }
}