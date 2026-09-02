using Application.Common.Pagination;
using Application.Requests;
using Application.Requests.Authorization;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Requests;

public sealed class RequestReadRepository : IRequestReadRepository
{
    private static readonly RequestStatus[] OverdueStatuses =
    {
        RequestStatus.Submitted,
        RequestStatus.InReview,
        RequestStatus.Assigned,
        RequestStatus.InProgress,
        RequestStatus.OnHold
    };

    private readonly ApplicationDbContext _context;

    public RequestReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<RequestDto>> SearchAsync(
        RequestQueryCriteria criteria,
        int page,
        int pageSize,
        RequestSortOptions sortOptions,
        Guid currentUserId,
        UserRole currentRole,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_context.Requests.AsNoTracking().AsQueryable(), criteria);
        query = RequestAuthorization.ApplyVisibilityFilter(query, currentUserId, currentRole);

        var totalItems = await query.CountAsync(cancellationToken);

        var ordered = ApplyOrdering(query, sortOptions);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new RequestDto(
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
                request.AreaId,
                request.Area.Name,
                request.RequestTypeId,
                request.RequestType.Name,
                request.RequesterId,
                request.Requester.Username,
                request.Requester.Email,
                request.ResponsibleId,
                request.Responsible != null ? request.Responsible.Username : null,
                request.Responsible != null ? request.Responsible.Email : null))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<RequestDto>(items, page, pageSize, totalItems);
    }

    public async Task<RequestDetailDto?> GetDetailAsync(
        Guid requestId,
        Guid currentUserId,
        UserRole currentRole,
        CancellationToken cancellationToken)
    {
        if (requestId == Guid.Empty)
        {
            return null;
        }

        var request = await _context.Requests
            .AsNoTracking()
            .Include(r => r.Area)
            .Include(r => r.RequestType)
            .Include(r => r.Requester)
            .Include(r => r.Responsible)
            .Include(r => r.StatusHistory)
                .ThenInclude(entry => entry.ChangedBy)
            .Include(r => r.Comments)
                .ThenInclude(comment => comment.Author)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return null;
        }

        if (!RequestAuthorization.CanViewRequest(request, currentUserId, currentRole))
        {
            return null;
        }

        var history = request.StatusHistory
            .OrderBy(entry => entry.Date)
            .Select(entry => new RequestStatusHistoryDto(
                entry.Id,
                entry.PreviousStatus,
                entry.NewStatus,
                entry.Date,
                entry.Comment,
                entry.ChangedById,
                entry.ChangedBy.Username))
            .ToList();

        var visibleComments = request.Comments
            .Where(comment => RequestAuthorization.CanViewComment(
                comment,
                currentUserId,
                currentRole,
                request.RequesterId))
            .OrderBy(comment => comment.Date)
            .Select(comment => new RequestCommentDto(
                comment.Id,
                comment.Text,
                comment.Visibility,
                comment.Date,
                comment.AuthorId,
                comment.Author.Username))
            .ToList();

        return new RequestDetailDto(
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
            visibleComments);
    }

    public async Task<Request?> FindEntityByIdAsync(Guid requestId, CancellationToken cancellationToken)
    {
        if (requestId == Guid.Empty)
        {
            return null;
        }

        return await _context.Requests
            .Include(r => r.Area)
            .Include(r => r.RequestType)
            .Include(r => r.Requester)
            .Include(r => r.Responsible)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestStatusHistoryDto>> GetStatusHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var query = _context.RequestStatusHistory
            .AsNoTracking()
            .Where(entry => entry.RequestId == requestId);

        return await query
            .OrderBy(entry => entry.Date)
            .Select(entry => new RequestStatusHistoryDto(
                entry.Id,
                entry.PreviousStatus,
                entry.NewStatus,
                entry.Date,
                entry.Comment,
                entry.ChangedById,
                entry.ChangedBy.Username))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RequestCommentDto>> GetCommentsAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var query = _context.RequestComments
            .AsNoTracking()
            .Where(comment => comment.RequestId == requestId);

        return await query
            .OrderBy(comment => comment.Date)
            .Select(comment => new RequestCommentDto(
                comment.Id,
                comment.Text,
                comment.Visibility,
                comment.Date,
                comment.AuthorId,
                comment.Author.Username))
            .ToListAsync(cancellationToken);
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

    public Task<int> CountOverdueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return _context.Requests
            .AsNoTracking()
            .Where(request => request.DueDate.HasValue
                && request.DueDate.Value < now
                && request.ClosedAt == null
                && OverdueStatuses.Contains(request.Status))
            .CountAsync(cancellationToken);
    }

    private static IQueryable<Request> ApplyFilters(
        IQueryable<Request> query,
        RequestQueryCriteria criteria)
    {
        if (criteria.Status.HasValue)
        {
            var status = criteria.Status.Value;
            query = query.Where(request => request.Status == status);
        }

        if (criteria.Priority.HasValue)
        {
            var priority = criteria.Priority.Value;
            query = query.Where(request => request.Priority == priority);
        }

        if (criteria.AreaId.HasValue)
        {
            var areaId = criteria.AreaId.Value;
            query = query.Where(request => request.AreaId == areaId);
        }

        if (criteria.RequestTypeId.HasValue)
        {
            var requestTypeId = criteria.RequestTypeId.Value;
            query = query.Where(request => request.RequestTypeId == requestTypeId);
        }

        if (criteria.RequesterId.HasValue)
        {
            var requesterId = criteria.RequesterId;
            query = query.Where(request => request.RequesterId == requesterId);
        }

        if (criteria.ResponsibleId.HasValue)
        {
            var responsibleId = criteria.ResponsibleId;
            query = query.Where(request => request.ResponsibleId == responsibleId);
        }

        if (criteria.FromDate.HasValue)
        {
            var fromDate = criteria.FromDate.Value;
            query = query.Where(request => request.CreatedAt >= fromDate);
        }

        if (criteria.ToDate.HasValue)
        {
            var toDate = criteria.ToDate.Value;
            query = query.Where(request => request.CreatedAt <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Code))
        {
            var code = criteria.Code.Trim();
            query = query.Where(request =>
                EF.Functions.Like(request.Code, $"%{code}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            query = query.Where(request =>
                EF.Functions.Like(request.Code, $"%{term}%")
                || EF.Functions.Like(request.Title, $"%{term}%")
                || EF.Functions.Like(request.Description, $"%{term}%"));
        }

        return query;
    }

    private static IQueryable<Request> ApplyOrdering(
        IQueryable<Request> query,
        RequestSortOptions sortOptions)
    {
        var descending = sortOptions.Direction == RequestSortDirection.Descending;

        return sortOptions.Field switch
        {
            RequestSortField.Code => descending
                ? query.OrderByDescending(r => r.Code).ThenBy(r => r.Id)
                : query.OrderBy(r => r.Code).ThenBy(r => r.Id),
            RequestSortField.Title => descending
                ? query.OrderByDescending(r => r.Title).ThenBy(r => r.Id)
                : query.OrderBy(r => r.Title).ThenBy(r => r.Id),
            RequestSortField.Priority => descending
                ? query.OrderByDescending(r => r.Priority).ThenBy(r => r.Id)
                : query.OrderBy(r => r.Priority).ThenBy(r => r.Id),
            RequestSortField.Status => descending
                ? query.OrderByDescending(r => r.Status).ThenBy(r => r.Id)
                : query.OrderBy(r => r.Status).ThenBy(r => r.Id),
            RequestSortField.DueDate => descending
                ? query.OrderByDescending(r => r.DueDate).ThenBy(r => r.Id)
                : query.OrderBy(r => r.DueDate).ThenBy(r => r.Id),
            _ => descending
                ? query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
                : query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
        };
    }
}