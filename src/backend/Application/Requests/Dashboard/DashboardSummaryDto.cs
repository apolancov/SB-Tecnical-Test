using Domain.Enums;

namespace Application.Requests.Dashboard;

public sealed record DashboardSummaryDto(
    int TotalRequests,
    IReadOnlyDictionary<RequestStatus, int> RequestsByStatus,
    IReadOnlyDictionary<RequestPriority, int> RequestsByPriority,
    int OverdueRequests,
    int PendingRequests,
    int AssignedRequests,
    int UnassignedRequests,
    IReadOnlyList<RequestSummaryDto> RecentRequests,
    DateTime GeneratedAtUtc);