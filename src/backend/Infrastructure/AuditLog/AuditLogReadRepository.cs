using Application.AuditLog;
using Application.AuditLog.GetAuditLogEntries;
using Application.Common.Pagination;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AuditLog;

public sealed class AuditLogReadRepository : IAuditLogReadRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<AuditLogEntryDto>> SearchAsync(
        AuditLogQueryCriteria criteria,
        int page,
        int pageSize,
        AuditLogSortOptions sortOptions,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLog
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, criteria);

        query = ApplySorting(query, sortOptions);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new AuditLogEntryDto(
                entry.Id,
                entry.Timestamp,
                entry.Action,
                entry.Outcome,
                entry.EntityType,
                entry.EntityId,
                entry.Details,
                entry.IpAddress,
                entry.ActorUserId,
                entry.ActorUserName))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<AuditLogEntryDto>(items, page, pageSize, totalItems);
    }

    public async Task<AuditLogEntryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var entry = await _context.AuditLog
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (entry is null)
        {
            return null;
        }

        return new AuditLogEntryDto(
            entry.Id,
            entry.Timestamp,
            entry.Action,
            entry.Outcome,
            entry.EntityType,
            entry.EntityId,
            entry.Details,
            entry.IpAddress,
            entry.ActorUserId,
            entry.ActorUserName);
    }

    private static IQueryable<Domain.Entities.AuditLogEntry> ApplyFilters(
        IQueryable<Domain.Entities.AuditLogEntry> query,
        AuditLogQueryCriteria criteria)
    {
        if (criteria.FromDate.HasValue)
        {
            var fromDate = criteria.FromDate.Value;
            query = query.Where(entry => entry.Timestamp >= fromDate);
        }

        if (criteria.ToDate.HasValue)
        {
            var toDate = criteria.ToDate.Value;
            query = query.Where(entry => entry.Timestamp <= toDate);
        }

        if (criteria.ActorUserId.HasValue)
        {
            var actorId = criteria.ActorUserId.Value;
            query = query.Where(entry => entry.ActorUserId == actorId);
        }

        if (criteria.Action.HasValue)
        {
            var action = criteria.Action.Value;
            query = query.Where(entry => entry.Action == action);
        }

        if (criteria.Outcome.HasValue)
        {
            var outcome = criteria.Outcome.Value;
            query = query.Where(entry => entry.Outcome == outcome);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            var entityType = criteria.EntityType;
            query = query.Where(entry => entry.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search;
            query = query.Where(entry =>
                (entry.EntityId != null && EF.Functions.Like(entry.EntityId, $"%{search}%"))
                || (entry.Details != null && EF.Functions.Like(entry.Details, $"%{search}%"))
                || (entry.ActorUserName != null && EF.Functions.Like(entry.ActorUserName, $"%{search}%")));
        }

        return query;
    }

    private static IQueryable<Domain.Entities.AuditLogEntry> ApplySorting(
        IQueryable<Domain.Entities.AuditLogEntry> query,
        AuditLogSortOptions sortOptions)
    {
        var descending = sortOptions.Direction == AuditLogSortDirection.Descending;

        IOrderedQueryable<Domain.Entities.AuditLogEntry> ordered = sortOptions.Field switch
        {
            AuditLogSortField.Action => descending
                ? query.OrderByDescending(entry => entry.Action)
                : query.OrderBy(entry => entry.Action),
            AuditLogSortField.Outcome => descending
                ? query.OrderByDescending(entry => entry.Outcome)
                : query.OrderBy(entry => entry.Outcome),
            AuditLogSortField.EntityType => descending
                ? query.OrderByDescending(entry => entry.EntityType)
                : query.OrderBy(entry => entry.EntityType),
            AuditLogSortField.ActorUserName => descending
                ? query.OrderByDescending(entry => entry.ActorUserName)
                : query.OrderBy(entry => entry.ActorUserName),
            _ => descending
                ? query.OrderByDescending(entry => entry.Timestamp)
                : query.OrderBy(entry => entry.Timestamp),
        };

        return ordered.ThenBy(entry => entry.Id);
    }
}