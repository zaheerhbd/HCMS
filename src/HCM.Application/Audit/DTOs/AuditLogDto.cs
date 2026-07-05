namespace HCM.Application.Audit.DTOs;

public class AuditLogDto
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedBy { get; set; }
    public string? ChangedByUserName { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class AuditLogListDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<AuditLogDto> Items { get; set; } = [];
}

public class AuditLogFilterDto
{
    public string? EntityType { get; set; }
    public string? ChangedBy { get; set; }    // username or user ID
    public string? Action { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
