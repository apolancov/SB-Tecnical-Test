using Application.Common.Pagination;
using Application.Common.Users;
using Application.Requests;
using Domain.Entities;
using Domain.Enums;

namespace Application.Requests.TestUtilities;

public sealed class InMemoryRequestReadRepository : IRequestReadRepository
{
    private readonly List<Request> _requests = new();

    public InMemoryRequestReadRepository(IEnumerable<Request>? requests = null)
    {
        if (requests is not null)
        {
            _requests.AddRange(requests);
        }
    }

    public int SearchCallCount { get; private set; }
    public int GetDetailCallCount { get; private set; }
    public int FindEntityCallCount { get; private set; }

    public Task<PaginatedResult<RequestDto>> SearchAsync(
        RequestQueryCriteria criteria,
        int page,
        int pageSize,
        RequestSortOptions sortOptions,
        Guid currentUserId,
        UserRole currentRole,
        CancellationToken cancellationToken)
    {
        SearchCallCount++;

        IEnumerable<Request> filtered = _requests;

        if (criteria.Status.HasValue)
        {
            var status = criteria.Status.Value;
            filtered = filtered.Where(r => r.Status == status);
        }

        if (criteria.Priority.HasValue)
        {
            var priority = criteria.Priority.Value;
            filtered = filtered.Where(r => r.Priority == priority);
        }

        if (criteria.AreaId.HasValue)
        {
            var areaId = criteria.AreaId.Value;
            filtered = filtered.Where(r => r.AreaId == areaId);
        }

        if (criteria.RequestTypeId.HasValue)
        {
            var requestTypeId = criteria.RequestTypeId.Value;
            filtered = filtered.Where(r => r.RequestTypeId == requestTypeId);
        }

        if (criteria.RequesterId.HasValue)
        {
            var requesterId = criteria.RequesterId.Value;
            filtered = filtered.Where(r => r.RequesterId == requesterId);
        }

        if (criteria.ResponsibleId.HasValue)
        {
            var responsibleId = criteria.ResponsibleId.Value;
            filtered = filtered.Where(r => r.ResponsibleId == responsibleId);
        }

        if (criteria.FromDate.HasValue)
        {
            var from = criteria.FromDate.Value;
            filtered = filtered.Where(r => r.CreatedAt >= from);
        }

        if (criteria.ToDate.HasValue)
        {
            var to = criteria.ToDate.Value;
            filtered = filtered.Where(r => r.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Code))
        {
            var code = criteria.Code.Trim();
            filtered = filtered.Where(r =>
                r.Code.Contains(code, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            filtered = filtered.Where(r =>
                r.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (currentRole != UserRole.Admin && currentRole != UserRole.Analista)
        {
            filtered = filtered.Where(r =>
                r.RequesterId == currentUserId
                || r.ResponsibleId == currentUserId);
        }

        var descending = sortOptions.Direction == RequestSortDirection.Descending;
        filtered = sortOptions.Field switch
        {
            RequestSortField.Code => descending
                ? filtered.OrderByDescending(r => r.Code)
                : filtered.OrderBy(r => r.Code),
            RequestSortField.Title => descending
                ? filtered.OrderByDescending(r => r.Title)
                : filtered.OrderBy(r => r.Title),
            RequestSortField.Priority => descending
                ? filtered.OrderByDescending(r => r.Priority)
                : filtered.OrderBy(r => r.Priority),
            RequestSortField.Status => descending
                ? filtered.OrderByDescending(r => r.Status)
                : filtered.OrderBy(r => r.Status),
            RequestSortField.DueDate => descending
                ? filtered.OrderByDescending(r => r.DueDate)
                : filtered.OrderBy(r => r.DueDate),
            _ => descending
                ? filtered.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
                : filtered.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
        };

        var ordered = filtered.ToList();
        var totalItems = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RequestDto(
                r.Id,
                r.Code,
                r.Title,
                r.Description,
                r.Priority,
                r.Status,
                r.CreatedAt,
                r.DueDate,
                r.EvidenceUrl,
                r.ClosedAt,
                r.AreaId,
                r.Area.Name,
                r.RequestTypeId,
                r.RequestType.Name,
                r.RequesterId,
                r.Requester.Username,
                r.Requester.Email,
                r.ResponsibleId,
                r.Responsible?.Username,
                r.Responsible?.Email))
            .ToList();

        return Task.FromResult(new PaginatedResult<RequestDto>(items, page, pageSize, totalItems));
    }

    public Task<RequestDetailDto?> GetDetailAsync(
        Guid requestId,
        Guid currentUserId,
        UserRole currentRole,
        CancellationToken cancellationToken)
    {
        GetDetailCallCount++;

        if (requestId == Guid.Empty)
        {
            return Task.FromResult<RequestDetailDto?>(null);
        }

        var request = _requests.FirstOrDefault(r => r.Id == requestId);
        if (request is null)
        {
            return Task.FromResult<RequestDetailDto?>(null);
        }

        var isRequesterOrResponsible =
            request.RequesterId == currentUserId
            || request.ResponsibleId == currentUserId;

        if (currentRole != UserRole.Admin && currentRole != UserRole.Analista
            && !isRequesterOrResponsible)
        {
            return Task.FromResult<RequestDetailDto?>(null);
        }

        var history = request.StatusHistory
            .OrderBy(h => h.Date)
            .Select(h => new RequestStatusHistoryDto(
                h.Id,
                h.PreviousStatus,
                h.NewStatus,
                h.Date,
                h.Comment,
                h.ChangedById,
                h.ChangedBy.Username))
            .ToList();

        var comments = request.Comments
            .OrderBy(c => c.Date)
            .Where(c => c.Visibility == CommentVisibility.Requester
                || currentRole == UserRole.Admin
                || currentRole == UserRole.Analista)
            .Select(c => new RequestCommentDto(
                c.Id,
                c.Text,
                c.Visibility,
                c.Date,
                c.AuthorId,
                c.Author.Username))
            .ToList();

        return Task.FromResult<RequestDetailDto?>(new RequestDetailDto(
            request.Id,
            request.Code,
            request.Title,
            request.Description,
            request.Priority,
            request.Status,
            request.CreatedAt,
            request.DueDate,
            request.EvidenceUrl,
            request.ClosedAt,
            new RequestUserDto(
                request.Requester.Id,
                request.Requester.Username,
                request.Requester.Email,
                request.Requester.Role),
            request.Responsible is null
                ? null
                : new RequestUserDto(
                    request.Responsible.Id,
                    request.Responsible.Username,
                    request.Responsible.Email,
                    request.Responsible.Role),
            new RequestLookupDto(request.Area.Id, request.Area.Name),
            new RequestLookupDto(request.RequestType.Id, request.RequestType.Name),
            history,
            comments));
    }

    public Task<Request?> FindEntityByIdAsync(Guid requestId, CancellationToken cancellationToken)
    {
        FindEntityCallCount++;

        if (requestId == Guid.Empty)
        {
            return Task.FromResult<Request?>(null);
        }

        return Task.FromResult(_requests.FirstOrDefault(r => r.Id == requestId));
    }

    public Task<IReadOnlyList<RequestStatusHistoryDto>> GetStatusHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var request = _requests.FirstOrDefault(r => r.Id == requestId);
        if (request is null)
        {
            return Task.FromResult<IReadOnlyList<RequestStatusHistoryDto>>(Array.Empty<RequestStatusHistoryDto>());
        }

        IReadOnlyList<RequestStatusHistoryDto> history = request.StatusHistory
            .OrderBy(h => h.Date)
            .Select(h => new RequestStatusHistoryDto(
                h.Id,
                h.PreviousStatus,
                h.NewStatus,
                h.Date,
                h.Comment,
                h.ChangedById,
                h.ChangedBy.Username))
            .ToList();

        return Task.FromResult(history);
    }

    public Task<IReadOnlyList<RequestCommentDto>> GetCommentsAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var request = _requests.FirstOrDefault(r => r.Id == requestId);
        if (request is null)
        {
            return Task.FromResult<IReadOnlyList<RequestCommentDto>>(Array.Empty<RequestCommentDto>());
        }

        IReadOnlyList<RequestCommentDto> comments = request.Comments
            .OrderBy(c => c.Date)
            .Select(c => new RequestCommentDto(
                c.Id,
                c.Text,
                c.Visibility,
                c.Date,
                c.AuthorId,
                c.Author.Username))
            .ToList();

        return Task.FromResult(comments);
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

    public Task<int> CountOverdueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pending = new[]
        {
            RequestStatus.Submitted,
            RequestStatus.InReview,
            RequestStatus.Assigned,
            RequestStatus.InProgress,
            RequestStatus.OnHold
        };

        var count = _requests.Count(r =>
            r.DueDate.HasValue
            && r.DueDate.Value < now
            && r.ClosedAt is null
            && pending.Contains(r.Status));

        return Task.FromResult(count);
    }

    public void Add(Request request) => _requests.Add(request);
}

public sealed class InMemoryRequestWriteRepository : IRequestWriteRepository
{
    public List<Request> AddedRequests { get; } = new();
    public List<Request> UpdatedRequests { get; } = new();
    public int AddCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }

    public Task AddAsync(Request request, CancellationToken cancellationToken)
    {
        AddCallCount++;
        AddedRequests.Add(request);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Request request, CancellationToken cancellationToken)
    {
        UpdateCallCount++;
        UpdatedRequests.Add(request);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAreaReadRepository : IAreaReadRepository
{
    private readonly List<Area> _areas;

    public InMemoryAreaReadRepository(params Area[] areas)
    {
        _areas = areas.ToList();
    }

    public Task<Area?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_areas.FirstOrDefault(a => a.Id == id));
    }

    public Task<IReadOnlyList<Area>> ListActiveAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Area> active = _areas
            .Where(area => area.IsActive)
            .OrderBy(area => area.Name)
            .ToList();

        return Task.FromResult(active);
    }
}

public sealed class InMemoryRequestTypeReadRepository : IRequestTypeReadRepository
{
    private readonly List<RequestType> _types;

    public InMemoryRequestTypeReadRepository(params RequestType[] types)
    {
        _types = types.ToList();
    }

    public Task<RequestType?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_types.FirstOrDefault(t => t.Id == id));
    }

    public Task<IReadOnlyList<RequestType>> ListActiveAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RequestType> active = _types
            .Where(type => type.IsActive)
            .OrderBy(type => type.Name)
            .ToList();

        return Task.FromResult(active);
    }
}

public sealed class InMemoryUserLookupRepository : IUserLookupRepository
{
    private readonly List<User> _users;

    public InMemoryUserLookupRepository(params User[] users)
    {
        _users = users.ToList();
    }

    public IReadOnlyCollection<User> Users => _users;

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
    }
}

public sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public Guid CurrentUserId { get; set; }
    public UserRole CurrentRole { get; set; } = UserRole.User;

    public Guid GetCurrentUserId() => CurrentUserId;

    public UserRole GetCurrentUserRole() => CurrentRole;
}

public sealed class SequenceRequestCodeGenerator : IRequestCodeGenerator
{
    private long _counter;

    public string LastCode { get; private set; } = string.Empty;

    public Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        _counter++;
        var year = DateTime.UtcNow.Year;
        LastCode = $"SOL-{year:D4}-{_counter:D4}";
        return Task.FromResult(LastCode);
    }
}

public sealed class InMemoryNotificationSender : Application.Requests.Notifications.INotificationSender
{
    public List<RequestNotification> Sent { get; } = new();

    public Task SendAsync(
        RequestNotification notification,
        CancellationToken cancellationToken = default)
    {
        Sent.Add(notification);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryStaffCandidateRepository : Application.Requests.Lookups.IStaffCandidateRepository
{
    private readonly List<User> _users;

    public InMemoryStaffCandidateRepository(params User[] users)
    {
        _users = users.ToList();
    }

    public Task<IReadOnlyList<User>> ListStaffCandidatesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<User> staff = _users
            .Where(user => user.IsActive
                && (user.Role == UserRole.Admin || user.Role == UserRole.Analista))
            .ToList();

        return Task.FromResult(staff);
    }
}

public sealed class NullAuditLogger : Application.Common.Audit.IAuditLogger
{
    public Task LogAsync(
        Application.Common.Audit.AuditLogContext context,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}