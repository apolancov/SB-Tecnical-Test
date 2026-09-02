using Application.Requests;
using Application.Requests.Dashboard;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Requests;

public sealed class DashboardReadRepository : IDashboardReadRepository
{
    private static readonly RequestStatus[] PendingStatuses =
    {
        RequestStatus.Submitted,
        RequestStatus.InReview,
        RequestStatus.Assigned,
        RequestStatus.InProgress,
        RequestStatus.OnHold
    };

    private readonly ApplicationDbContext _context;

    public DashboardReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> CountRequestsAsync(CancellationToken cancellationToken)
    {
        return _context.Requests.AsNoTracking().CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken)
    {
        var grouped = await _context.Requests
            .AsNoTracking()
            .GroupBy(request => request.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var dictionary = grouped.ToDictionary(item => item.Status, item => item.Count);

        foreach (RequestStatus status in Enum.GetValues<RequestStatus>())
        {
            dictionary.TryAdd(status, 0);
        }

        return dictionary;
    }

    public async Task<IReadOnlyDictionary<RequestPriority, int>> CountByPriorityAsync(
        CancellationToken cancellationToken)
    {
        var grouped = await _context.Requests
            .AsNoTracking()
            .GroupBy(request => request.Priority)
            .Select(group => new { Priority = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var dictionary = grouped.ToDictionary(item => item.Priority, item => item.Count);

        foreach (RequestPriority priority in Enum.GetValues<RequestPriority>())
        {
            dictionary.TryAdd(priority, 0);
        }

        return dictionary;
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken)
    {
        return _context.Requests
            .AsNoTracking()
            .Where(request => PendingStatuses.Contains(request.Status))
            .CountAsync(cancellationToken);
    }

    public Task<int> CountAssignedAsync(CancellationToken cancellationToken)
    {
        return _context.Requests
            .AsNoTracking()
            .Where(request => request.ResponsibleId != null)
            .CountAsync(cancellationToken);
    }

    public Task<int> CountUnassignedAsync(CancellationToken cancellationToken)
    {
        return _context.Requests
            .AsNoTracking()
            .Where(request => request.ResponsibleId == null)
            .CountAsync(cancellationToken);
    }

    public Task<int> CountByStatusAsync(RequestStatus status, CancellationToken cancellationToken)
    {
        return _context.Requests
            .AsNoTracking()
            .Where(request => request.Status == status)
            .CountAsync(cancellationToken);
    }

    public Task<int> CountByPriorityAsync(RequestPriority priority, CancellationToken cancellationToken)
    {
        return _context.Requests
            .AsNoTracking()
            .Where(request => request.Priority == priority)
            .CountAsync(cancellationToken);
    }

    public Task<int> CountOverdueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return _context.Requests
            .AsNoTracking()
            .Where(request => request.DueDate.HasValue
                && request.DueDate.Value < now
                && request.ClosedAt == null
                && PendingStatuses.Contains(request.Status))
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RequestSummaryDto>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return Array.Empty<RequestSummaryDto>();
        }

        return await _context.Requests
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .ThenBy(request => request.Id)
            .Take(count)
            .Select(request => new RequestSummaryDto(
                request.Id,
                request.Code,
                request.Title,
                request.Priority,
                request.Status,
                request.CreatedAt,
                request.DueDate,
                request.RequesterId,
                request.Requester.Username,
                request.ResponsibleId,
                request.Responsible != null ? request.Responsible.Username : null))
            .ToListAsync(cancellationToken);
    }
}