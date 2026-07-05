using HCM.Application.Audit.DTOs;
using HCM.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Audit.DataHandlers;

public class AuditDataHandler : IAuditDataHandler
{
    private readonly IApplicationDbContext _db;

    public AuditDataHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLogListDto> GetAuditLogsAsync(AuditLogFilterDto filter, CancellationToken ct = default)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 20;

        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(a => a.Action == filter.Action);

        if (!string.IsNullOrWhiteSpace(filter.ChangedBy))
            query = query.Where(a => a.ChangedBy != null && a.ChangedBy.Contains(filter.ChangedBy));

        if (filter.From.HasValue)
            query = query.Where(a => a.ChangedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(a => a.ChangedAt <= filter.To.Value);

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(a => a.ChangedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        // Enrich ChangedBy with username where available
        var userIds = logs
            .Where(l => l.ChangedBy != null && Guid.TryParse(l.ChangedBy, out _))
            .Select(l => Guid.Parse(l.ChangedBy!))
            .Distinct()
            .ToHashSet();

        var userNames = userIds.Count > 0
            ? await _db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id.ToString(), u => u.Username, ct)
            : [];

        var items = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Action = l.Action,
            OldValues = l.OldValues,
            NewValues = l.NewValues,
            ChangedBy = l.ChangedBy,
            ChangedByUserName = l.ChangedBy != null && userNames.TryGetValue(l.ChangedBy, out var name) ? name : l.ChangedBy,
            ChangedAt = l.ChangedAt
        }).ToList();

        return new AuditLogListDto
        {
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            Items = items
        };
    }
}
