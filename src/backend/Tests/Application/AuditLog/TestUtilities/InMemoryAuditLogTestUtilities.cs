using Application.AuditLog;
using Application.AuditLog.GetAuditLogEntries;
using Application.Common.Pagination;
using Domain.Entities;
using Domain.Enums;

namespace Application.AuditLog.TestUtilities;

public sealed class InMemoryAuditLogReadRepository : IAuditLogReadRepository
{
    private readonly List<AuditLogEntry> _entries;

    public InMemoryAuditLogReadRepository(params AuditLogEntry[] entries)
    {
        _entries = entries.ToList();
    }

    public IReadOnlyList<AuditLogEntry> Entries => _entries;

    public Task<PaginatedResult<AuditLogEntryDto>> SearchAsync(
        AuditLogQueryCriteria criteria,
        int page,
        int pageSize,
        AuditLogSortOptions sortOptions,
        CancellationToken cancellationToken)
    {
        IEnumerable<AuditLogEntry> query = _entries;

        if (criteria.FromDate.HasValue)
        {
            var from = criteria.FromDate.Value;
            query = query.Where(entry => entry.Timestamp >= from);
        }

        if (criteria.ToDate.HasValue)
        {
            var to = criteria.ToDate.Value;
            query = query.Where(entry => entry.Timestamp <= to);
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
                (entry.EntityId != null && entry.EntityId.Contains(search, StringComparison.Ordinal))
                || (entry.Details != null && entry.Details.Contains(search, StringComparison.Ordinal))
                || (entry.ActorUserName != null && entry.ActorUserName.Contains(search, StringComparison.Ordinal)));
        }

        var ordered = sortOptions.Field switch
        {
            AuditLogSortField.Action => sortOptions.Direction == AuditLogSortDirection.Descending
                ? query.OrderByDescending(e => e.Action)
                : (IOrderedEnumerable<AuditLogEntry>)query.OrderBy(e => e.Action),
            AuditLogSortField.Outcome => sortOptions.Direction == AuditLogSortDirection.Descending
                ? query.OrderByDescending(e => e.Outcome)
                : query.OrderBy(e => e.Outcome),
            AuditLogSortField.EntityType => sortOptions.Direction == AuditLogSortDirection.Descending
                ? query.OrderByDescending(e => e.EntityType)
                : query.OrderBy(e => e.EntityType),
            AuditLogSortField.ActorUserName => sortOptions.Direction == AuditLogSortDirection.Descending
                ? query.OrderByDescending(e => e.ActorUserName)
                : query.OrderBy(e => e.ActorUserName),
            _ => sortOptions.Direction == AuditLogSortDirection.Descending
                ? query.OrderByDescending(e => e.Timestamp)
                : query.OrderBy(e => e.Timestamp),
        };

        ordered = ordered.ThenBy(entry => entry.Id);

        var materialized = ordered.ToList();
        var totalItems = materialized.Count;
        var items = materialized
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
            .ToList();

        return Task.FromResult(new PaginatedResult<AuditLogEntryDto>(items, page, pageSize, totalItems));
    }

    public Task<AuditLogEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult<AuditLogEntryDto?>(null);
        }

        var entry = _entries.FirstOrDefault(item => item.Id == id);
        if (entry is null)
        {
            return Task.FromResult<AuditLogEntryDto?>(null);
        }

        return Task.FromResult<AuditLogEntryDto?>(new AuditLogEntryDto(
            entry.Id,
            entry.Timestamp,
            entry.Action,
            entry.Outcome,
            entry.EntityType,
            entry.EntityId,
            entry.Details,
            entry.IpAddress,
            entry.ActorUserId,
            entry.ActorUserName));
    }
}

public sealed class RecordingAuditLogger : Application.Common.Audit.IAuditLogger
{
    public List<Application.Common.Audit.AuditLogContext> Recorded { get; } = new();

    public Task LogAsync(
        Application.Common.Audit.AuditLogContext context,
        CancellationToken cancellationToken)
    {
        Recorded.Add(context);
        return Task.CompletedTask;
    }
}

public sealed class StaticAuditContextAccessor : Application.Common.Audit.IAuditContextAccessor
{
    public StaticAuditContextAccessor(string? ipAddress)
    {
        IpAddress = ipAddress;
    }

    public string? IpAddress { get; }

    public string? GetClientIpAddress() => IpAddress;
}