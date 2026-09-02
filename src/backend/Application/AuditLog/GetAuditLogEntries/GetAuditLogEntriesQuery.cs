using Application.Common.Pagination;
using Domain.Enums;

namespace Application.AuditLog.GetAuditLogEntries;

public sealed record GetAuditLogEntriesQuery(
    int Page,
    int PageSize,
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? ActorUserId,
    AuditAction? Action,
    AuditOutcome? Outcome,
    string? EntityType,
    string? Search,
    AuditLogSortField SortField,
    AuditLogSortDirection SortDirection);